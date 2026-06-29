using EventHub.Api.Domain.Entities;
using EventHub.Api.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace EventHub.Tests.Entities;

public class BookingTests
{
    [Fact]
    public void CreateBooking_ValidParameters_SetsPendingStatusAndCreatedAt()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        Guid eventId = Guid.NewGuid();
        DateTimeOffset now = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        FakeTimeProvider timeProvider = new(now);
        DateTime expectedCreatedAt = new(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);

        // Act
        Booking booking = new(id, eventId, timeProvider);

        // Assert
        booking.Id.Should().Be(id);
        booking.EventId.Should().Be(eventId);
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.ProcessedAt.Should().BeNull();
        booking.CreatedAt.Should().Be(expectedCreatedAt);
    }

    [Fact]
    public void Confirm_PendingBooking_SetsConfirmedStatusAndProcessedAt()
    {
        // Arrange
        DateTimeOffset now = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        FakeTimeProvider timeProvider = new(now);
        Booking booking = new(Guid.NewGuid(), Guid.NewGuid(), timeProvider);
        DateTime expectedProcessedAt = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        // Act
        timeProvider.Advance(TimeSpan.FromHours(2));
        booking.Confirm(timeProvider);

        // Assert
        booking.Status.Should().Be(BookingStatus.Confirmed);

        booking.ProcessedAt.Should().Be(expectedProcessedAt);
    }

    [Fact]
    public void Reject_PendingBooking_SetsRejectedStatusAndProcessedAt()
    {
        // Arrange
        DateTimeOffset now = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        FakeTimeProvider timeProvider = new(now);
        Booking booking = new(Guid.NewGuid(), Guid.NewGuid(), timeProvider);
        DateTime expectedProcessedAt = new(2026, 6, 1, 13, 0, 0, DateTimeKind.Utc);

        // Act
        timeProvider.Advance(TimeSpan.FromHours(3));
        booking.Reject(timeProvider);

        // Assert
        booking.Status.Should().Be(BookingStatus.Rejected);

        booking.ProcessedAt.Should().Be(expectedProcessedAt);
    }
}