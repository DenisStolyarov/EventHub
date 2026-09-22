using System.Linq.Expressions;
using EventHub.Bookings.Domain.Entities;
using EventHub.Bookings.Domain.Enums;

namespace EventHub.Bookings.Domain.Specifications;

public static class BookingSpecifications
{
    public static readonly TimeSpan ActiveBookingsWindow = TimeSpan.FromHours(24);

    public static Expression<Func<Booking, bool>> IsActiveForUser(Guid userId, DateTime now)
    {
        DateTime activeSince = now - ActiveBookingsWindow;

        return b => b.UserId == userId
            && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
            && b.CreatedAt >= activeSince;
    }
}
