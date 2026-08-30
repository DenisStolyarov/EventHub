using EventHub.Application.Abstractions.Identity;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Dtos.Bookings;
using EventHub.Application.Exceptions;
using EventHub.Domain.Entities;
using EventHub.Domain.Services;

namespace EventHub.Application.Services;

public sealed class BookingService(BookingManager bookingManager, ICurrentUserService currentUser, IUnitOfWork unitOfWork) : IBookingService
{
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<BookingInfo> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            Guid userId = currentUser.Id ?? throw new UnauthorizedException();

            Event @event = await unitOfWork.Events.GetByIdAsync(eventId, cancellationToken)
                ?? throw new NotFoundException(nameof(Event), eventId);

            Booking booking = await bookingManager.CreateAsync(@event, userId, cancellationToken);

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
