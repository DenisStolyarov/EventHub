namespace EventHub.Shared.Contracts;

public sealed record BookingCreated(
    Guid Id,
    Guid BookingId,
    Guid EventId) : IIntegrationEvent;