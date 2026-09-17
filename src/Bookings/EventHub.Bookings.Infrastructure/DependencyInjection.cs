using EventHub.Bookings.Application.Abstractions.Persistence;
using EventHub.Bookings.Domain.Abstractions;
using EventHub.Bookings.Infrastructure.Persistence;
using EventHub.Bookings.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Bookings.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BookingsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("BookingsDatabase")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IBookingCounter, BookingRepository>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        BookingsDbContext dbContext = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
