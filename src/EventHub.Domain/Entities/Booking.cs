using EventHub.Domain.Enums;
using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Entities;

public class Booking
{
    public Guid Id { get; }

    public Guid EventId { get; }

    public Guid UserId { get; }

    public BookingStatus Status { get; private set; }

    public DateTime CreatedAt { get; }

    public DateTime? ProcessedAt { get; private set; }

    public Event Event { get; private set; } = null!;

    public User User { get; private set; } = null!;

    private Booking() { }

    public Booking(Guid id, Guid eventId, Guid userId, TimeProvider? timeProvider = null)
    {
        if (eventId == Guid.Empty)
        {
            throw new ValidationException(nameof(EventId), "EventId cannot be empty.");
        }

        if (userId == Guid.Empty)
        {
            throw new ValidationException(nameof(UserId), "UserId cannot be empty.");
        }

        TimeProvider tp = timeProvider ?? TimeProvider.System;

        Id = id;
        EventId = eventId;
        UserId = userId;
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
