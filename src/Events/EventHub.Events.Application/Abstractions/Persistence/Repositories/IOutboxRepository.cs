using EventHub.Shared.Contracts;

namespace EventHub.Events.Application.Abstractions.Persistence.Repositories;

public interface IOutboxRepository
{
    void Add<TMessage>(TMessage message) where TMessage : IIntegrationEvent;
}
