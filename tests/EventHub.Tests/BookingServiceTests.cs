using EventHub.Api.Application.Dto.Bookings;
using EventHub.Api.Application.Exceptions;
using EventHub.Api.Application.Services;
using EventHub.Api.Domain.Entities;
using EventHub.Api.Domain.Enums;
using EventHub.Api.Domain.Interfaces;
using EventHub.Api.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace EventHub.Tests;

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
        Event @event = CreateEvent(Guid.NewGuid());

        _eventRepository.Setup(r => r.GetById(@event.Id)).Returns(@event);

        // Act
        BookingInfo result = await _service.CreateBookingAsync(@event.Id);

        // Assert
        result.Should().BeEquivalentTo(new
        {
            EventId = @event.Id,
            Status = BookingStatus.Pending,
        });

        result.Id.Should().NotBeEmpty();

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
        Event @event = CreateEvent(Guid.NewGuid());

        _eventRepository.Setup(r => r.GetById(@event.Id)).Returns(@event);

        // Act
        BookingInfo first = await _service.CreateBookingAsync(@event.Id);
        BookingInfo second = await _service.CreateBookingAsync(@event.Id);

        // Assert
        first.Id.Should().NotBeEmpty();
        second.Id.Should().NotBeEmpty();
        first.Id.Should().NotBe(second.Id);
        first.EventId.Should().Be(@event.Id);
        second.EventId.Should().Be(@event.Id);

        _bookingRepository.Verify(r => r.Add(It.IsAny<Booking>()), Times.Exactly(2));
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

    private static Event CreateEvent(Guid id)
    {
        Period period = new(
            new(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc),
            new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc)
        );

        return new(id, "Event title", "Event description", period);
    }
}