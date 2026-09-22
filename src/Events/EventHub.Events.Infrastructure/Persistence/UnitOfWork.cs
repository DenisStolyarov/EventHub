using EventHub.Events.Application.Abstractions.Persistence;
using EventHub.Events.Application.Abstractions.Persistence.Repositories;
using EventHub.Events.Infrastructure.Persistence.Repositories;

namespace EventHub.Events.Infrastructure.Persistence;

public sealed class UnitOfWork(EventsDbContext context, TimeProvider timeProvider) : IUnitOfWork
{
    private readonly Lazy<IEventRepository> _events = new(() => new EventRepository(context));

    private readonly Lazy<IInboxRepository> _inbox = new(() => new InboxRepository(context, timeProvider));

    private readonly Lazy<IOutboxRepository> _outbox = new(() => new OutboxRepository(context, timeProvider));

    public IEventRepository Events => _events.Value;

    public IInboxRepository Inbox => _inbox.Value;

    public IOutboxRepository Outbox => _outbox.Value;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    public void Dispose() => context.Dispose();

    public ValueTask DisposeAsync() => context.DisposeAsync();
}
