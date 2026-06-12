using EventHub.Api.Domain.Enums;

namespace EventHub.Api.Domain.Entities;

public class Booking
{
    public Guid Id { get; }

    public Guid EventId { get; }

    public BookingStatus Status { get; private set; }

    public DateTime CreatedAt { get; }

    public DateTime? ProcessedAt { get; private set; }

    public Booking(Guid id, Guid eventId, TimeProvider? timeProvider = null)
    {
        TimeProvider tp = timeProvider ?? TimeProvider.System;

        Id = id;
        EventId = eventId;
        Status = BookingStatus.Pending;
        CreatedAt = tp.GetUtcNow().UtcDateTime;
    }

    public void Confirm(TimeProvider? timeProvider = null)
    {
        TimeProvider tp = timeProvider ?? TimeProvider.System;

        Status = BookingStatus.Confirmed;
        ProcessedAt = tp.GetUtcNow().UtcDateTime;
    }

    public void Reject(TimeProvider? timeProvider = null)
    {
        TimeProvider tp = timeProvider ?? TimeProvider.System;
        
        Status = BookingStatus.Rejected;
        ProcessedAt = tp.GetUtcNow().UtcDateTime;
    }
}