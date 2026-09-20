using EventHub.Events.Application.Abstractions.Persistence.Repositories;

namespace EventHub.Events.Application.Abstractions.Persistence;

public interface IUnitOfWork : IAsyncDisposable, IDisposable
{
    IEventRepository Events { get; }

    IInboxRepository Inbox { get; }

    IOutboxRepository Outbox { get; }

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
