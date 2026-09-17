using EventHub.Bookings.Application.Abstractions.Services;
using EventHub.Bookings.Application.Services;
using EventHub.Bookings.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Bookings.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IBookingService, BookingService>();

        services.AddTransient<BookingManager>();

        return services;
    }
}
