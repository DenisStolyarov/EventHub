using EventHub.Users.Application.Abstractions.Persistence.Repositories;
using EventHub.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Users.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(UsersDbContext context) : IUserRepository
{
    public async Task<User?> GetByLoginAsync(string login, CancellationToken cancellationToken = default) =>
        await context.Users.FirstOrDefaultAsync(u => u.Login == login, cancellationToken);

    public async Task<bool> ExistsByLoginAsync(string login, CancellationToken cancellationToken = default) =>
        await context.Users.AnyAsync(u => u.Login == login, cancellationToken);

    public void Add(User user) => context.Users.Add(user);
}
