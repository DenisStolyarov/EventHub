using EventHub.Bookings.Domain.Abstractions;
using EventHub.Bookings.Domain.Entities;
using EventHub.Bookings.Domain.Exceptions;

using static EventHub.Bookings.Domain.Specifications.BookingSpecifications;

namespace EventHub.Bookings.Domain.Services;

public sealed class BookingManager(IBookingCounter bookingCounter, TimeProvider timeProvider)
{
    public const int MaxActiveBookingsAllowed = 10;

    public async Task<Booking> CreateAsync(
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        int activeBookingsCount = await bookingCounter.CountAsync(
            IsActiveForUser(userId, now),
            cancellationToken);

        if (activeBookingsCount >= MaxActiveBookingsAllowed)
        {
            throw new MaxActiveBookingsExceededException(MaxActiveBookingsAllowed);
        }

        return new Booking(Guid.CreateVersion7(), eventId, userId, now);
    }
}
