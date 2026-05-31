using EventHub.Api.Domain.Entities;

namespace EventHub.Api.Application.Dto.Bookings;

public static class BookingMappingExtensions
{
    public static BookingInfo ToInfo(this Booking booking) => new()
    {
        Id = booking.Id,
        EventId = booking.EventId,
        Status = booking.Status,
    };
}