using EventHub.Users.Application.Abstractions.Services;
using EventHub.Users.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Users.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
