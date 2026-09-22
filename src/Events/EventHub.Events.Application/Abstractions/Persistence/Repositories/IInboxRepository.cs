namespace EventHub.Events.Application.Abstractions.Persistence.Repositories;

public interface IInboxRepository
{
    void Add(Guid id);
}
