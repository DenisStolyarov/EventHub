using EventHub.Api.Application.Interfaces;
using EventHub.Api.Application.Services;

namespace EventHub.Api.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();

        return services;
    }
}