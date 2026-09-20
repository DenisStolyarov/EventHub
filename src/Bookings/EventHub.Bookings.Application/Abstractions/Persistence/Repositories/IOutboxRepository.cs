using EventHub.Shared.Contracts;

namespace EventHub.Bookings.Application.Abstractions.Persistence.Repositories;

public interface IOutboxRepository
{
    void Add<TMessage>(TMessage message) where TMessage : IIntegrationEvent;
}
