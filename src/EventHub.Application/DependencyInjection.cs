using EventHub.Application.Abstractions.Services;
using EventHub.Application.Services;
using EventHub.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<BookingManager>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();

        return services;
    }
}
