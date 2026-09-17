using EventHub.Events.Application.Abstractions.Services;
using EventHub.Events.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Events.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IEventService, EventService>();

        return services;
    }
}
