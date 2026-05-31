using EventHub.Api.Domain.Interfaces;
using EventHub.Api.Infrastructure.Repositories;

namespace EventHub.Api.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IEventRepository, InMemoryEventRepository>();
        services.AddSingleton<IBookingRepository, InMemoryBookingRepository>();

        return services;
    }
}