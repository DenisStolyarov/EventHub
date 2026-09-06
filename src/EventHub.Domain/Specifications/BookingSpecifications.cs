using System.Linq.Expressions;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;

namespace EventHub.Domain.Specifications;

public static class BookingSpecifications
{
    public static Expression<Func<Booking, bool>> IsActiveForUser(Guid userId, DateTime now) =>
        b => b.Event.StartAt > now
            && b.UserId == userId
            && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed);
}
