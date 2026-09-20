namespace EventHub.Shared.Contracts;

public sealed record EventCancelled(
    Guid Id,
    Guid EventId) : IIntegrationEvent;