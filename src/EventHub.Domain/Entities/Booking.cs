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

    internal Booking(Guid id, Guid eventId, Guid userId, DateTime createdAt)
    {
        if (eventId == Guid.Empty)
        {
            throw new ValidationException(nameof(EventId), "EventId cannot be empty.");
        }

        if (userId == Guid.Empty)
        {
            throw new ValidationException(nameof(UserId), "UserId cannot be empty.");
        }

        Id = id;
        EventId = eventId;
        UserId = userId;
        Status = BookingStatus.Pending;
        CreatedAt = createdAt;
    }

    public void Confirm(DateTime processedAt)
    {
        EnsureActive();

        Status = BookingStatus.Confirmed;
        ProcessedAt = processedAt;
    }

    public void Reject(DateTime processedAt)
    {
        EnsureActive();

        Status = BookingStatus.Rejected;
        ProcessedAt = processedAt;
    }

    public void Cancel()
    {
        EnsureActive();

        Status = BookingStatus.Cancelled;
    }

    private void EnsureActive()
    {
        if (Status is BookingStatus.Cancelled)
        {
            throw new DomainException("Booking is cancelled.");
        }
    }
}
