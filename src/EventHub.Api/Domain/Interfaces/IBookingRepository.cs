using EventHub.Domain.Entities;

namespace EventHub.Api.Domain.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ICollection<Guid>> GetPendingBookingIdsAsync(CancellationToken cancellationToken = default);

    void Add(Booking booking);
}