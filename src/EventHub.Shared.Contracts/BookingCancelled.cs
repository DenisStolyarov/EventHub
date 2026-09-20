namespace EventHub.Shared.Contracts;

public sealed record BookingCancelled(
    Guid Id,
    Guid EventId) : IIntegrationEvent;