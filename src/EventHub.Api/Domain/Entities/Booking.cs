using EventHub.Api.Domain.Enums;

namespace EventHub.Api.Domain.Entities;

public class Booking
{
    public Guid Id { get; }

    public Guid EventId { get; }

    public BookingStatus Status { get; private set; }

    public DateTime CreatedAt { get; }

    public DateTime? ProcessedAt { get; private set; }

    public Booking(Guid id, Guid eventId)
    {
        Id = id;
        EventId = eventId;
        Status = BookingStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }
}