using EventHub.Users.Application.Abstractions.Persistence.Repositories;

namespace EventHub.Users.Application.Abstractions.Persistence;

public interface IUnitOfWork : IAsyncDisposable, IDisposable
{
    IUserRepository Users { get; }

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
