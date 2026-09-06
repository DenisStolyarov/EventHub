using EventHub.Domain.Entities;
using EventHub.Infrastructure.Persistence;

namespace EventHub.IntegrationTests.TestData;

public static class AppDbContextExtensions
{
    public static async Task<User> CreateUserAsync(
        this AppDbContext ctx,
        CancellationToken cancellationToken = default)
    {
        User user = EntityProvider.CreateUser();

        ctx.Users.Add(user);
        await ctx.SaveChangesAsync(cancellationToken);

        return user;
    }
}
