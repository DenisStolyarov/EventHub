using EventHub.Api.Domain.Entities;
using EventHub.Api.Domain.Enums;
using EventHub.Api.Domain.Interfaces;

namespace EventHub.Api.Infrastructure.Repositories;

public class InMemoryBookingRepository : IBookingRepository
{
    private readonly Lock _lock = new();
    private readonly List<Booking> _bookings = [];

    public IEnumerable<Booking> GetAll()
    {
        lock (_lock)
        {
            return [.. _bookings];
        }
    }

    public IEnumerable<Booking> GetPendingBookings()
    {
        lock (_lock)
        {
            return [.. _bookings.Where(booking => booking.Status is BookingStatus.Pending)];
        }
    }

    public Booking? GetById(Guid id)
    {
        lock (_lock)
        {
            return _bookings.Find(b => b.Id == id);
        }
    }

    public void Add(Booking booking)
    {
        lock (_lock)
        {
            _bookings.Add(booking);
        }
    }

    public void Update(Booking booking)
    {
        lock (_lock)
        {
            int index = _bookings.FindIndex(b => b.Id == booking.Id);

            if (index < 0)
            {
                throw new KeyNotFoundException($"Booking with id '{booking.Id}' not found.");
            }

            _bookings[index] = booking;
        }
    }

    public void Delete(Guid id)
    {
        lock (_lock)
        {
            int index = _bookings.FindIndex(b => b.Id == id);

            if (index < 0)
            {
                throw new KeyNotFoundException($"Booking with id '{id}' not found.");
            }

            _bookings.RemoveAt(index);
        }
    }
}