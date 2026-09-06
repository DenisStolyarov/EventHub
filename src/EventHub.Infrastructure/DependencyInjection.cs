using EventHub.Application.Abstractions.Identity;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Domain.Abstractions;
using EventHub.Infrastructure.BackgroundServices;
using EventHub.Infrastructure.Identity;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IBookingCounter, BookingRepository>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IPasswordHasher, Sha256PasswordHasher>();

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
