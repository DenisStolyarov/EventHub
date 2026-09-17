using EventHub.Users.Domain.Entities;
using EventHub.Users.Infrastructure.Persistence;

namespace EventHub.Users.IntegrationTests.TestData;

public static class UsersDbContextExtensions
{
    public static async Task<User> CreateUserAsync(
        this UsersDbContext ctx,
        CancellationToken cancellationToken = default)
    {
        User user = EntityProvider.CreateUser();

        ctx.Users.Add(user);

        await ctx.SaveChangesAsync(cancellationToken);

        return user;
    }
}
