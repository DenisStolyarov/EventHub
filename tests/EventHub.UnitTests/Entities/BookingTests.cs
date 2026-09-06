using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using EventHub.Domain.Exceptions;
using FluentAssertions;

namespace EventHub.UnitTests.Entities;

public class BookingTests
{
    [Fact]
    public void CreateBooking_ValidParameters_SetsPendingStatusAndCreatedAt()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        Guid eventId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        DateTime createdAt = new(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);

        // Act
        Booking booking = new(id, eventId, userId, createdAt);

        // Assert
        booking.Id.Should().Be(id);
        booking.EventId.Should().Be(eventId);
        booking.UserId.Should().Be(userId);
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.ProcessedAt.Should().BeNull();
        booking.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Confirm_PendingBooking_SetsConfirmedStatusAndProcessedAt()
    {
        // Arrange
        DateTime createdAt = new(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        DateTime processedAt = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        Booking booking = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), createdAt);

        // Act
        booking.Confirm(processedAt);

        // Assert
        booking.Status.Should().Be(BookingStatus.Confirmed);

        booking.ProcessedAt.Should().Be(processedAt);
    }

    [Fact]
    public void Reject_PendingBooking_SetsRejectedStatusAndProcessedAt()
    {
        // Arrange
        DateTime createdAt = new(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        DateTime processedAt = new(2026, 6, 1, 13, 0, 0, DateTimeKind.Utc);
        Booking booking = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), createdAt);

        // Act
        booking.Reject(processedAt);

        // Assert
        booking.Status.Should().Be(BookingStatus.Rejected);

        booking.ProcessedAt.Should().Be(processedAt);
    }

    [Fact]
    public void Cancel_PendingBooking_SetsCancelledStatus()
    {
        // Arrange
        DateTime createdAt = new(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        Booking booking = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), createdAt);

        // Act
        booking.Cancel();

        // Assert
        booking.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public void Cancel_AlreadyCancelled_ThrowsDomainException()
    {
        // Arrange
        DateTime createdAt = new(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        Booking booking = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), createdAt);
        booking.Cancel();

        // Act
        Action act = () => booking.Cancel();

        // Assert
        act.Should().Throw<DomainException>();
    }
}
