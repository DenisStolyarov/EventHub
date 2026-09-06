using System.Linq.Expressions;
using EventHub.Domain.Entities;

namespace EventHub.Domain.Abstractions;

public interface IBookingCounter
{
    Task<int> CountAsync(Expression<Func<Booking, bool>> predicate, CancellationToken cancellationToken = default);
}
