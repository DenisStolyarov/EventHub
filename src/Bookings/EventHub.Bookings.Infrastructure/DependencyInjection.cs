using EventHub.Bookings.Application.Abstractions.Persistence;
using EventHub.Bookings.Application.IntegrationEvents;
using EventHub.Bookings.Domain.Abstractions;
using EventHub.Bookings.Infrastructure.Persistence;
using EventHub.Bookings.Infrastructure.Persistence.Repositories;
using EventHub.Shared.Contracts;
using EventHub.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Bookings.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddDatabase(configuration)
            .AddScoped<IBookingCounter, BookingRepository>()
            .AddKafka();

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BookingsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("BookingsDatabase")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    private static IServiceCollection AddKafka(this IServiceCollection services) =>
        services
            .AddKafkaMessaging()
            .AddKafkaTopicInitializer(
                Topics.EventSeatReserved,
                Topics.EventSeatUnavailable,
                Topics.EventCancelled)
            .AddOutboxDispatcher<OutboxRepository>()
            .AddKafkaConsumer<EventSeatReserved, EventSeatReservedHandler>(Topics.EventSeatReserved)
            .AddKafkaConsumer<EventSeatUnavailable, EventSeatUnavailableHandler>(Topics.EventSeatUnavailable)
            .AddKafkaConsumer<EventCancelled, EventCancelledHandler>(Topics.EventCancelled);

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        BookingsDbContext dbContext = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
