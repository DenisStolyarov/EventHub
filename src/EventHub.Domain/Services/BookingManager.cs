using EventHub.Domain.Abstractions;
using EventHub.Domain.Entities;
using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Services;

public sealed class BookingManager(IBookingCounter bookingCounter, TimeProvider timeProvider)
{
    public const int MaxActiveBookingsAllowed = 10;

    public async Task<Booking> CreateAsync(
        Event @event,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        if (now >= @event.StartAt)
        {
            throw new EventAlreadyStartedException();
        }

        int activeBookingsCount = await bookingCounter.CountAsync(
            b => b.Event.StartAt > now && b.UserId == userId,
            cancellationToken);

        if (activeBookingsCount >= MaxActiveBookingsAllowed)
        {
            throw new MaxActiveBookingsExceededException();
        }

        if (!@event.TryReserveSeats())
        {
            throw new NoAvailableSeatsException();
        }

        Booking booking = new(Guid.CreateVersion7(), @event.Id, userId, now);

        return booking;
    }
}
