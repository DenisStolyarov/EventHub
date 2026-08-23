using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Persistence.Repositories;
using EventHub.Infrastructure.Persistence.Repositories;

namespace EventHub.Infrastructure.Persistence;

public sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    private readonly Lazy<IEventRepository> _events = new(() => new EventRepository(context));
    private readonly Lazy<IBookingRepository> _bookings = new(() => new BookingRepository(context));

    public IEventRepository Events => _events.Value;

    public IBookingRepository Bookings => _bookings.Value;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    public void Dispose() => context.Dispose();

    public ValueTask DisposeAsync() => context.DisposeAsync();
}
