using EventHub.Api.Domain.Entities;

namespace EventHub.Api.Domain.Interfaces;

public interface IBookingRepository
{
    IEnumerable<Booking> GetAll();

    IEnumerable<Booking> GetPendingBookings();

    Booking? GetById(Guid id);

    void Add(Booking booking);

    void Update(Booking booking);

    void Delete(Guid id);
}