using EventHub.Api.Application.Dto.Bookings;
using EventHub.Api.Application.Exceptions;
using EventHub.Api.Application.Interfaces;
using EventHub.Api.Domain.Entities;
using EventHub.Api.Infrastructure.DataAccess;

namespace EventHub.Api.Application.Services;

public sealed class BookingService(AppDbContext context) : IBookingService
{
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<BookingInfo> CreateBookingAsync(Guid eventId)
    {
        await _semaphore.WaitAsync();

        try
        {
            Event @event = await context.Events.FindAsync(eventId)
                ?? throw new NotFoundException(nameof(Event), eventId);

            if (!@event.TryReserveSeats())
            {
                throw new NoAvailableSeatsException();
            }

            Booking booking = new(Guid.CreateVersion7(), eventId);

            context.Bookings.Add(booking);

            await context.SaveChangesAsync();

            return booking.ToInfo();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<BookingInfo> GetBookingByIdAsync(Guid bookingId)
    {
        Booking booking = await context.Bookings.FindAsync(bookingId)
            ?? throw new NotFoundException(nameof(Booking), bookingId);

        return booking.ToInfo();
    }
}
