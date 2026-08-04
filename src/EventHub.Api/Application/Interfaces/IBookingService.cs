using EventHub.Api.Application.Dto.Bookings;

namespace EventHub.Api.Application.Interfaces;

public interface IBookingService
{
    Task<BookingInfo> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task<BookingInfo> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
}