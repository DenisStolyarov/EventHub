using EventHub.Api.Domain.Enums;

namespace EventHub.Api.Application.Dto.Bookings;

public sealed record BookingInfo
{
    public required Guid Id { get; init; }

    public required Guid EventId { get; init; }

    public required BookingStatus Status { get; init; }
}