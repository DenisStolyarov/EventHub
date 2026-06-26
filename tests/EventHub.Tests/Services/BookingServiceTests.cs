using EventHub.Api.Application.Dto.Bookings;
using EventHub.Api.Application.Exceptions;
using EventHub.Api.Application.Services;
using EventHub.Api.Domain.Entities;
using EventHub.Api.Domain.Enums;
using EventHub.Api.Domain.Interfaces;
using EventHub.Api.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;

using static EventHub.Tests.TestUtilities.TestDateTime;

namespace EventHub.Tests.Services;

public class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepository;
    private readonly Mock<IEventRepository> _eventRepository;
    private readonly BookingService _service;

    public BookingServiceTests()
    {
        _bookingRepository = new();
        _eventRepository = new();
        _service = new(_bookingRepository.Object, _eventRepository.Object);
    }

    [Fact]
    public async Task CreateBookingAsync_EventExists_ReturnsPendingBookingInfo()
    {
        // Arrange
        Event @event = CreateEvent(Guid.NewGuid(), 1);

        _eventRepository.Setup(r => r.GetById(@event.Id)).Returns(@event);
        _eventRepository.Setup(r => r.Update(It.IsAny<Event>()));

        // Act
        BookingInfo result = await _service.CreateBookingAsync(@event.Id);

        // Assert
        result.Id.Should().NotBe(Guid.Empty);
        result.EventId.Should().Be(@event.Id);
        result.Status.Should().Be(BookingStatus.Pending);

        @event.AvailableSeats.Should().Be(0);

        _eventRepository.Verify(
            r => r.Update(It.Is<Event>(e => 
                e.Id == @event.Id &&
                e.AvailableSeats == 0)),
            Times.Once);

        _bookingRepository.Verify(
            r => r.Add(It.Is<Booking>(b =>
                b.Id == result.Id &&
                b.EventId == @event.Id &&
                b.Status == BookingStatus.Pending)),
            Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_SameEventBookedMultipleTimes_ReturnsUniqueIds()
    {
        // Arrange
        Event @event = CreateEvent(Guid.NewGuid(), 2);

        _eventRepository.Setup(r => r.GetById(@event.Id)).Returns(@event);
        _eventRepository.Setup(r => r.Update(It.IsAny<Event>()));

        // Act
        BookingInfo first = await _service.CreateBookingAsync(@event.Id);
        BookingInfo second = await _service.CreateBookingAsync(@event.Id);

        // Assert
        first.Id.Should().NotBe(second.Id);
        first.Id.Should().NotBe(Guid.Empty);
        second.Id.Should().NotBe(Guid.Empty);
        first.EventId.Should().Be(@event.Id);
        second.EventId.Should().Be(@event.Id);
        first.Status.Should().Be(BookingStatus.Pending);
        second.Status.Should().Be(BookingStatus.Pending);

        @event.AvailableSeats.Should().Be(0);

        _eventRepository.Verify(r => r.Update(It.IsAny<Event>()), Times.Exactly(2));
        _bookingRepository.Verify(r => r.Add(It.IsAny<Booking>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateBookingAsync_NotEnoughSeats_ThrowsNoAvailableSeatsException()
    {
        // Arrange
        Event @event = CreateEvent(Guid.NewGuid(), 1);

        _eventRepository.Setup(r => r.GetById(@event.Id)).Returns(@event);
        _eventRepository.Setup(r => r.Update(It.IsAny<Event>()));

        // Act
        BookingInfo first = await _service.CreateBookingAsync(@event.Id);
        Func<Task> second = () => _service.CreateBookingAsync(@event.Id);

        // Assert
        first.EventId.Should().Be(@event.Id);
        first.Status.Should().Be(BookingStatus.Pending);

        @event.AvailableSeats.Should().Be(0);

        await second.Should().ThrowAsync<NoAvailableSeatsException>();

        _eventRepository.Verify(r => r.Update(It.IsAny<Event>()), Times.Once);
        _bookingRepository.Verify(r => r.Add(It.IsAny<Booking>()), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_AllSeatsAlreadyReserved_ThrowsNoAvailableSeatsException()
    {
        // Arrange
        Event @event = CreateEvent(Guid.NewGuid(), 1);
        @event.TryReserveSeats();

        _eventRepository.Setup(r => r.GetById(@event.Id)).Returns(@event);

        // Act
        Func<Task> act = () => _service.CreateBookingAsync(@event.Id);

        // Assert
        await act.Should().ThrowAsync<NoAvailableSeatsException>();

        _eventRepository.Verify(r => r.Update(It.IsAny<Event>()), Times.Never);
        _bookingRepository.Verify(r => r.Add(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task GetBookingByIdAsync_BookingExists_ReturnsBookingInfo()
    {
        // Arrange
        Guid bookingId = Guid.NewGuid();
        Guid eventId = Guid.NewGuid();
        Booking booking = new(bookingId, eventId);

        _bookingRepository.Setup(r => r.GetById(bookingId)).Returns(booking);

        // Act
        BookingInfo result = await _service.GetBookingByIdAsync(bookingId);

        // Assert
        result.Should().BeEquivalentTo(new
        {
            Id = bookingId,
            EventId = eventId,
            Status = BookingStatus.Pending,
        });
    }

    [Fact]
    public async Task GetBookingByIdAsync_WhenBookingConfirmed_ReturnsUpdatedStatus()
    {
        // Arrange
        Guid bookingId = Guid.NewGuid();
        Guid eventId = Guid.NewGuid();
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero));
        Booking booking = new(bookingId, eventId, timeProvider);

        _bookingRepository
            .Setup(r => r.GetById(bookingId))
            .Returns(booking);

        BookingInfo before = await _service.GetBookingByIdAsync(bookingId);

        before.Status.Should().Be(BookingStatus.Pending);

        // Act
        timeProvider.Advance(TimeSpan.FromHours(2));
        booking.Confirm(timeProvider);

        // Assert
        BookingInfo after = await _service.GetBookingByIdAsync(bookingId);

        after.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WhenBookingRejected_ReturnsUpdatedStatus()
    {
        // Arrange
        Guid bookingId = Guid.NewGuid();
        Guid eventId = Guid.NewGuid();
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero));
        Booking booking = new(bookingId, eventId, timeProvider);

        _bookingRepository
            .Setup(r => r.GetById(bookingId))
            .Returns(booking);

        BookingInfo before = await _service.GetBookingByIdAsync(bookingId);

        before.Status.Should().Be(BookingStatus.Pending);

        // Act
        timeProvider.Advance(TimeSpan.FromHours(3));
        booking.Reject(timeProvider);

        // Assert
        BookingInfo after = await _service.GetBookingByIdAsync(bookingId);

        after.Status.Should().Be(BookingStatus.Rejected);
    }

    [Fact]
    public async Task CreateBookingAsync_EventDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        Guid eventId = Guid.NewGuid();

        _eventRepository.Setup(r => r.GetById(eventId)).Returns((Event?)null);

        // Act
        Func<Task> act = () => _service.CreateBookingAsync(eventId);

        // Assert
        await act.Should()
            .ThrowAsync<NotFoundException>()
            .Where(e => e.EntityId.Equals(eventId) && e.EntityName == nameof(Event));

        _eventRepository.Verify(r => r.Update(It.IsAny<Event>()), Times.Never);
        _bookingRepository.Verify(r => r.Add(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task GetBookingByIdAsync_BookingDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        Guid bookingId = Guid.NewGuid();

        _bookingRepository.Setup(r => r.GetById(bookingId)).Returns((Booking?)null);

        // Act
        Func<Task> act = () => _service.GetBookingByIdAsync(bookingId);

        // Assert
        await act.Should()
            .ThrowAsync<NotFoundException>()
            .Where(e => e.EntityId.Equals(bookingId) && e.EntityName == nameof(Booking));
    }

    private static Event CreateEvent(Guid id, int totalSeats = 100)
    {
        Period period = new(
            UtcDateTime(2026, 6, 1, 10),
            UtcDateTime(2026, 6, 1, 12)
        );

        return new(id, "Event title", "Event description", totalSeats, period);
    }
}