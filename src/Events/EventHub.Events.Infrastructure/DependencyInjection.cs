using EventHub.Events.Application.Abstractions.Persistence;
using EventHub.Events.Application.IntegrationEvents;
using EventHub.Events.Infrastructure.Persistence;
using EventHub.Events.Infrastructure.Persistence.Repositories;
using EventHub.Shared.Contracts;
using EventHub.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Events.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddDatabase(configuration)
            .AddKafka();

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EventsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("EventsDatabase")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    private static IServiceCollection AddKafka(this IServiceCollection services) =>
        services
            .AddKafkaMessaging()
            .AddKafkaTopicInitializer(
                Topics.BookingCreated,
                Topics.BookingCancelled)
            .AddOutboxDispatcher<OutboxRepository>()
            .AddKafkaConsumer<BookingCreated, BookingCreatedHandler>(Topics.BookingCreated)
            .AddKafkaConsumer<BookingCancelled, BookingCancelledHandler>(Topics.BookingCancelled);

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        EventsDbContext dbContext = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
