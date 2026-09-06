using EventHub.Application.Dtos.Bookings;

namespace EventHub.Application.Abstractions.Services;

public interface IBookingService
{
    Task<BookingInfo> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task<BookingInfo> CancelBookingAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task<BookingInfo> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
}
