using EventHub.Bookings.Application.Abstractions.Persistence.Repositories;

namespace EventHub.Bookings.Application.Abstractions.Persistence;

public interface IUnitOfWork : IAsyncDisposable, IDisposable
{
    IBookingRepository Bookings { get; }

    IInboxRepository Inbox { get; }

    IOutboxRepository Outbox { get; }

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
