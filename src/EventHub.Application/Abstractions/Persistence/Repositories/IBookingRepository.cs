using EventHub.Domain.Entities;

namespace EventHub.Application.Abstractions.Persistence.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ICollection<Guid>> GetPendingBookingIdsAsync(CancellationToken cancellationToken = default);

    void Add(Booking booking);
}
