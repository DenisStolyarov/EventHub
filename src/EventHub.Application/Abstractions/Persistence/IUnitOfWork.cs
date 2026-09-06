using EventHub.Application.Abstractions.Persistence.Repositories;

namespace EventHub.Application.Abstractions.Persistence;

public interface IUnitOfWork : IAsyncDisposable, IDisposable
{
    IBookingRepository Bookings { get; }

    IEventRepository Events { get; }

    IUserRepository Users { get; }

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
