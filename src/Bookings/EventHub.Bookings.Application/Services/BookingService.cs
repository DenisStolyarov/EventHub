using EventHub.Bookings.Application.Abstractions.Persistence;
using EventHub.Bookings.Application.Abstractions.Services;
using EventHub.Bookings.Application.Dtos.Bookings;
using EventHub.Bookings.Application.Exceptions;
using EventHub.Bookings.Domain.Entities;
using EventHub.Bookings.Domain.Services;
using EventHub.Shared.Authentication;

using static EventHub.Shared.Authentication.UserRoles;

namespace EventHub.Bookings.Application.Services;

public sealed class BookingService(BookingManager bookingManager, ICurrentUserService currentUser, IUnitOfWork unitOfWork) : IBookingService
{
    public async Task<BookingInfo> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        Guid userId = currentUser.Id ?? throw new UnauthorizedException();

        Booking booking = await bookingManager.CreateAsync(eventId, userId, cancellationToken);

        unitOfWork.Bookings.Add(booking);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return booking.ToInfo();
    }

    public async Task<BookingInfo> CancelBookingAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        Booking booking = await unitOfWork.Bookings.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), bookingId);

        EnsureCanAccess(booking);

        booking.Cancel();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return booking.ToInfo();
    }

    public async Task<BookingInfo> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        Booking booking = await unitOfWork.Bookings.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), bookingId);

        EnsureCanAccess(booking);

        return booking.ToInfo();
    }

    private void EnsureCanAccess(Booking booking)
    {
        if (currentUser.IsInRole(Admin))
        {
            return;
        }

        if (currentUser.Id is not Guid userId)
        {
            throw new UnauthorizedException();
        }

        if (booking.UserId != userId)
        {
            throw new ForbiddenException();
        }
    }
}
