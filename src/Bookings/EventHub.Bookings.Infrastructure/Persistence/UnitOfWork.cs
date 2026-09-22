using EventHub.Bookings.Application.Abstractions.Persistence;
using EventHub.Bookings.Application.Abstractions.Persistence.Repositories;
using EventHub.Bookings.Infrastructure.Persistence.Repositories;

namespace EventHub.Bookings.Infrastructure.Persistence;

public sealed class UnitOfWork(BookingsDbContext context, TimeProvider timeProvider) : IUnitOfWork
{
    private readonly Lazy<IBookingRepository> _bookings = new(() => new BookingRepository(context));

    private readonly Lazy<IInboxRepository> _inbox = new(() => new InboxRepository(context, timeProvider));

    private readonly Lazy<IOutboxRepository> _outbox = new(() => new OutboxRepository(context, timeProvider));

    public IBookingRepository Bookings => _bookings.Value;

    public IInboxRepository Inbox => _inbox.Value;

    public IOutboxRepository Outbox => _outbox.Value;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    public void Dispose() => context.Dispose();

    public ValueTask DisposeAsync() => context.DisposeAsync();
}
