using EventHub.Bookings.Domain.Entities;

namespace EventHub.Bookings.Application.Abstractions.Persistence.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ICollection<Booking>> GetActiveByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    void Add(Booking booking);
}
