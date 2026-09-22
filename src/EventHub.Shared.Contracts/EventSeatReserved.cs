namespace EventHub.Shared.Contracts;

public sealed record EventSeatReserved(
    Guid Id,
    Guid BookingId) : IIntegrationEvent;
