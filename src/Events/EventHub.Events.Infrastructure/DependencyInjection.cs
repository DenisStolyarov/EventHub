using EventHub.Events.Application.Abstractions.Caching;
using EventHub.Events.Application.Abstractions.Persistence;
using EventHub.Events.Application.IntegrationEvents;
using EventHub.Events.Infrastructure.Caching;
using EventHub.Events.Infrastructure.Persistence;
using EventHub.Events.Infrastructure.Persistence.Repositories;
using EventHub.Shared.Contracts;
using EventHub.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EventHub.Events.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddDatabase(configuration)
            .AddCache()
            .AddKafka();

    private static IServiceCollection AddCache(this IServiceCollection services)
    {
        services
            .AddOptionsWithValidateOnStart<RedisOptions>()
            .ValidateDataAnnotations()
            .BindConfiguration(RedisOptions.SectionName);

        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            RedisOptions options = provider.GetRequiredService<IOptions<RedisOptions>>().Value;

            ConfigurationOptions config = new()
            {
                ConnectTimeout = options.ConnectTimeout,
                Password = options.Password,
                SyncTimeout = options.SyncTimeout,
                AbortOnConnectFail = options.AbortOnConnectFail,
                ConnectRetry = options.ConnectRetry,
            };

            config.EndPoints.Add(options.Host, options.Port);

            return ConnectionMultiplexer.Connect(config);
        });

        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }

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
