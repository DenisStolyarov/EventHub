using EventHub.Application.Abstractions.Persistence.Repositories;
using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<User?> GetByLoginAsync(string login, CancellationToken cancellationToken = default) =>
        await context.Users.FirstOrDefaultAsync(u => u.Login == login, cancellationToken);

    public async Task<bool> ExistsByLoginAsync(string login, CancellationToken cancellationToken = default) =>
        await context.Users.AnyAsync(u => u.Login == login, cancellationToken);

    public void Add(User user) => context.Users.Add(user);
}
