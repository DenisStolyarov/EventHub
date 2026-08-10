using EventHub.Api.Infrastructure.BackgroundServices;
using EventHub.Api.Infrastructure.DataAccess;
using EventHub.Api.Infrastructure.Repositories;
using EventHub.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Api.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddHostedService<BookingProcessor>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}