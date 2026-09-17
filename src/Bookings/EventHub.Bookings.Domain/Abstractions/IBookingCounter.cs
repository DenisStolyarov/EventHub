using System.Linq.Expressions;
using EventHub.Bookings.Domain.Entities;

namespace EventHub.Bookings.Domain.Abstractions;

public interface IBookingCounter
{
    Task<int> CountAsync(Expression<Func<Booking, bool>> predicate, CancellationToken cancellationToken = default);
}
