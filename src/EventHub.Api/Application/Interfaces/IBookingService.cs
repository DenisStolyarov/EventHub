using EventHub.Api.Application.Dto.Bookings;

namespace EventHub.Api.Application.Interfaces;

public interface IBookingService
{
    Task<Guid> CreateBookingAsync(Guid eventId);

    Task<BookingInfo> GetBookingByIdAsync(Guid bookingId);
}