using EventHub.Api.Domain.Interfaces;
using EventHub.Api.Infrastructure.BackgroundServices;
using EventHub.Api.Infrastructure.DataAccess;
using EventHub.Api.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Api.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddHostedService<BookingProcessor>();

        services.AddSingleton<IEventRepository, InMemoryEventRepository>();
        services.AddSingleton<IBookingRepository, InMemoryBookingRepository>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureCreatedAsync();
    }
}
