using EventHub.Api.Domain.Entities;
using EventHub.Api.Domain.Enums;
using FluentAssertions;

namespace EventHub.Tests;

public class BookingTests
{
    [Fact]
    public void CreateBooking_ValidParameters_SetsPendingStatus()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        Guid eventId = Guid.NewGuid();

        // Act
        Booking booking = new(id, eventId);

        // Assert
        booking.Id.Should().Be(id);
        booking.EventId.Should().Be(eventId);
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.ProcessedAt.Should().BeNull();
        booking.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Confirm_PendingBooking_SetsConfirmedStatusAndProcessedAt()
    {
        // Arrange
        Booking booking = new(Guid.NewGuid(), Guid.NewGuid());

        // Act
        booking.Confirm();

        // Assert
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Reject_PendingBooking_SetsRejectedStatusAndProcessedAt()
    {
        // Arrange
        Booking booking = new(Guid.NewGuid(), Guid.NewGuid());

        // Act
        booking.Reject();

        // Assert
        booking.Status.Should().Be(BookingStatus.Rejected);
        booking.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}