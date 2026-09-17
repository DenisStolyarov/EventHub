using EventHub.Events.Application.Abstractions.Persistence;
using EventHub.Events.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Events.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EventsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("EventsDatabase")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        EventsDbContext dbContext = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
