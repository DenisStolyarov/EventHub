using EventHub.Application.Abstractions.Persistence.Repositories;

namespace EventHub.Application.Abstractions.Persistence;

public interface IUnitOfWork : IAsyncDisposable, IDisposable
{
    IEventRepository Events { get; }

    IBookingRepository Bookings { get; }

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}