using System.Linq.Expressions;
using EventHub.Bookings.Domain.Entities;
using EventHub.Bookings.Domain.Enums;

namespace EventHub.Bookings.Domain.Specifications;

public static class BookingSpecifications
{
    public static Expression<Func<Booking, bool>> IsActiveForUser(Guid userId) =>
        b => b.UserId == userId
            && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed);
}
