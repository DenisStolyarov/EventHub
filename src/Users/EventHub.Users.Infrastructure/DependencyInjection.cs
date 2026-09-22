using EventHub.Users.Application.Abstractions.Identity;
using EventHub.Users.Application.Abstractions.Persistence;
using EventHub.Users.Infrastructure.Identity;
using EventHub.Users.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Users.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<UsersDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("UsersDatabase")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services
            .AddOptionsWithValidateOnStart<JwtIssuerOptions>()
            .ValidateDataAnnotations()
            .BindConfiguration(JwtIssuerOptions.SectionName);

        services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IPasswordHasher, Sha256PasswordHasher>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        UsersDbContext dbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
