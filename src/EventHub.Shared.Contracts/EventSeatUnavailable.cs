namespace EventHub.Shared.Contracts;

public sealed record EventSeatUnavailable(
    Guid Id,
    Guid BookingId,
    string Reason) : IIntegrationEvent;