using System.Linq.Expressions;
using EventHub.Domain.Abstractions;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Services;
using EventHub.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;

using static EventHub.UnitTests.TestData.TestDateTime;

namespace EventHub.UnitTests.Services;

public sealed class BookingManagerTests
{
    [Fact]
    public async Task CreateAsync_ValidEvent_ReturnsPendingBooking()
    {
        // Arrange
        const int totalSeats = 10;
        const int expectedSeats = 9;

        Guid userId = Guid.NewGuid();
        FakeTimeProvider timeProvider = new(UtcDate(2026, 5, 1, 10));
        Event @event = CreateEvent(startAt: UtcDateTime(2026, 6, 1, 10), totalSeats);
        Mock<IBookingCounter> counterMock = CreateCounterMock(0);
        BookingManager manager = new(counterMock.Object, timeProvider);

        // Act
        Booking booking = await manager.CreateAsync(@event, userId, TestContext.Current.CancellationToken);

        // Assert
        booking.Should().NotBeNull();
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.EventId.Should().Be(@event.Id);
        booking.UserId.Should().Be(userId);

        @event.AvailableSeats.Should().Be(expectedSeats);

        counterMock.Verify(
            c => c.CountAsync(
                It.IsAny<Expression<Func<Booking, bool>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        BookingManager manager = CreateManagerWithActiveBookings(0);

        // Act
        Func<Task> act = () => manager.CreateAsync(null!, userId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateAsync_EventAlreadyStarted_ThrowsEventAlreadyStartedException()
    {
        // Arrange
        const int totalSeats = 10;

        Guid userId = Guid.NewGuid();
        FakeTimeProvider timeProvider = new(UtcDate(2026, 6, 1, 12));
        Event @event = CreateEvent(startAt: UtcDateTime(2026, 6, 1, 10), totalSeats);
        BookingManager manager = CreateManagerWithActiveBookings(0, timeProvider);

        // Act
        Func<Task> act = () => manager.CreateAsync(@event, userId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<EventAlreadyStartedException>();

        @event.AvailableSeats.Should().Be(totalSeats);
    }

    [Fact]
    public async Task CreateAsync_EventStartsNow_ThrowsEventAlreadyStartedException()
    {
        // Arrange
        const int totalSeats = 10;

        Guid userId = Guid.NewGuid();
        FakeTimeProvider timeProvider = new(UtcDate(2026, 6, 1, 10));
        Event @event = CreateEvent(startAt: UtcDateTime(2026, 6, 1, 10), totalSeats);
        BookingManager manager = CreateManagerWithActiveBookings(0, timeProvider);

        // Act
        Func<Task> act = () => manager.CreateAsync(@event, userId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<EventAlreadyStartedException>();

        @event.AvailableSeats.Should().Be(totalSeats);
    }

    [Fact]
    public async Task CreateAsync_NoAvailableSeats_ThrowsNoAvailableSeatsException()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        FakeTimeProvider timeProvider = new(UtcDate(2026, 5, 1, 10));
        Event @event = CreateEvent(startAt: UtcDateTime(2026, 6, 1, 10), totalSeats: 1);
        BookingManager manager = CreateManagerWithActiveBookings(0, timeProvider);

        // Act
        Booking first = await manager.CreateAsync(@event, userId, TestContext.Current.CancellationToken);
        Func<Task> second = () => manager.CreateAsync(@event, userId, TestContext.Current.CancellationToken);

        // Assert
        first.Should().NotBeNull();

        await second.Should().ThrowAsync<NoAvailableSeatsException>();
    }

    [Fact]
    public async Task CreateAsync_LimitExceeded_ThrowsAndDoesNotReserveSeats()
    {
        // Arrange
        const int totalSeats = 5;

        Guid userId = Guid.NewGuid();
        FakeTimeProvider timeProvider = new(UtcDate(2026, 5, 1, 10));
        Event @event = CreateEvent(startAt: UtcDateTime(2026, 6, 1, 10), totalSeats);
        BookingManager manager = CreateManagerWithActiveBookings(BookingManager.MaxActiveBookingsAllowed, timeProvider);

        // Act
        Func<Task> act = () => manager.CreateAsync(@event, userId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<MaxActiveBookingsExceededException>();

        @event.AvailableSeats.Should().Be(totalSeats);
    }

    [Fact]
    public async Task CreateAsync_ActiveBookingsOneBelowLimit_ReturnsBooking()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        FakeTimeProvider timeProvider = new(UtcDate(2026, 5, 1, 10));
        Event @event = CreateEvent(startAt: UtcDateTime(2026, 6, 1, 10));
        BookingManager manager = CreateManagerWithActiveBookings(BookingManager.MaxActiveBookingsAllowed - 1, timeProvider);

        // Act
        Booking booking = await manager.CreateAsync(@event, userId, TestContext.Current.CancellationToken);

        // Assert
        booking.Should().NotBeNull();
        booking.EventId.Should().Be(@event.Id);
        booking.UserId.Should().Be(userId);
        booking.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task CreateAsync_DifferentUsersHaveIndependentLimits()
    {
        // Arrange
        Guid user1 = Guid.NewGuid();
        Guid user2 = Guid.NewGuid();
        FakeTimeProvider timeProvider = new(UtcDate(2026, 5, 1, 10));
        Event @event = CreateEvent(startAt: UtcDateTime(2026, 6, 1, 10));
        Mock<IBookingCounter> counterMock = new();
        counterMock.SetupSequence(c => c.CountAsync(It.IsAny<Expression<Func<Booking, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(10)
            .ReturnsAsync(0);
        BookingManager manager = new(counterMock.Object, timeProvider);

        // Act
        Func<Task> user1Act = () => manager.CreateAsync(@event, user1, TestContext.Current.CancellationToken);
        Task<Booking> user2Act() => manager.CreateAsync(@event, user2, TestContext.Current.CancellationToken);

        // Assert
        await user1Act.Should().ThrowAsync<MaxActiveBookingsExceededException>();

        Booking user2Booking = await user2Act();

        user2Booking.Should().NotBeNull();
        user2Booking.UserId.Should().Be(user2);
    }

    private static BookingManager CreateManagerWithActiveBookings(int activeCount, TimeProvider? timeProvider = null) =>
        new(CreateCounterMock(activeCount).Object, timeProvider ?? TimeProvider.System);

    private static Mock<IBookingCounter> CreateCounterMock(int activeCount = 0)
    {
        Mock<IBookingCounter> mock = new();

        mock.Setup(c => c.CountAsync(It.IsAny<Expression<Func<Booking, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeCount);

        return mock;
    }

    private static Event CreateEvent(DateTime startAt, int totalSeats = 10)
    {
        Period period = new(startAt, startAt.AddHours(2));

        return new Event(Guid.NewGuid(), "Event title", "Event description", totalSeats, period);
    }
}
