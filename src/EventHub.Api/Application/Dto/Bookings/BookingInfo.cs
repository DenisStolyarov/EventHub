using EventHub.Api.Domain.Enums;

namespace EventHub.Api.Application.Dto.Bookings;

public sealed record BookingInfo
{
    public required BookingStatus Status { get; init; }
}