using EventHub.Users.Application.Abstractions.Persistence;
using EventHub.Users.Application.Abstractions.Persistence.Repositories;
using EventHub.Users.Infrastructure.Persistence.Repositories;

namespace EventHub.Users.Infrastructure.Persistence;

public sealed class UnitOfWork(UsersDbContext context) : IUnitOfWork
{
    private readonly Lazy<IUserRepository> _users = new(() => new UserRepository(context));

    public IUserRepository Users => _users.Value;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    public void Dispose() => context.Dispose();

    public ValueTask DisposeAsync() => context.DisposeAsync();
}
