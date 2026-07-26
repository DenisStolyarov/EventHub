using EventHub.Api.Application.Dto.Bookings;
using EventHub.Api.Application.Exceptions;
using EventHub.Api.Application.Interfaces;
using EventHub.Api.Domain.Entities;
using EventHub.Api.Domain.Interfaces;

namespace EventHub.Api.Application.Services;

public sealed class BookingService(IUnitOfWork unitOfWork) : IBookingService
{
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<BookingInfo> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            Event @event = await unitOfWork.Events.GetByIdAsync(eventId, cancellationToken)
                ?? throw new NotFoundException(nameof(Event), eventId);

            if (!@event.TryReserveSeats())
            {
                throw new NoAvailableSeatsException();
            }

            Booking booking = new(Guid.CreateVersion7(), eventId);

            unitOfWork.Bookings.Add(booking);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return booking.ToInfo();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<BookingInfo> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        Booking booking = await unitOfWork.Bookings.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), bookingId);

        return booking.ToInfo();
    }
}