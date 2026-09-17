using EventHub.Events.Application.Abstractions.Persistence;
using EventHub.Events.Application.Abstractions.Persistence.Repositories;
using EventHub.Events.Infrastructure.Persistence.Repositories;

namespace EventHub.Events.Infrastructure.Persistence;

public sealed class UnitOfWork(EventsDbContext context) : IUnitOfWork
{
    private readonly Lazy<IEventRepository> _events = new(() => new EventRepository(context));

    public IEventRepository Events => _events.Value;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    public void Dispose() => context.Dispose();

    public ValueTask DisposeAsync() => context.DisposeAsync();
}
