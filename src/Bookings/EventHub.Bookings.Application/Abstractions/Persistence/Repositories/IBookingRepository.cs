using EventHub.Bookings.Domain.Entities;

namespace EventHub.Bookings.Application.Abstractions.Persistence.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(Booking booking);
}
