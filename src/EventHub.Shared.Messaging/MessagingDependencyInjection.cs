using EventHub.Shared.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventHub.Shared.Messaging;

public static class MessagingDependencyInjection
{
    public static IServiceCollection AddKafkaMessaging(this IServiceCollection services)
    {
        services
            .AddOptionsWithValidateOnStart<MessagingOptions>()
            .ValidateDataAnnotations()
            .BindConfiguration(MessagingOptions.SectionName);

        services.AddSingleton<KafkaProducer>();

        return services;
    }

    public static IServiceCollection AddOutboxDispatcher<TProcessor>(this IServiceCollection services)
        where TProcessor : class, IOutboxProcessor
    {
        services.AddScoped<IOutboxProcessor, TProcessor>();

        services.AddHostedService<OutboxDispatcher>();

        return services;
    }

    public static IServiceCollection AddKafkaTopicInitializer(this IServiceCollection services, params string[] topics)
    {
        services.AddHostedService(sp => new KafkaTopicInitializer(
            sp.GetRequiredService<IOptions<MessagingOptions>>(),
            sp.GetRequiredService<ILogger<KafkaTopicInitializer>>(),
            topics));

        return services;
    }

    public static IServiceCollection AddKafkaConsumer<TMessage, THandler>(this IServiceCollection services, string topic)
        where TMessage : class, IIntegrationEvent
        where THandler : class, IIntegrationMessageHandler<TMessage>
    {
        services.AddHostedService(sp => new KafkaConsumerService<TMessage, THandler>(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IOptions<MessagingOptions>>(),
            sp.GetRequiredService<ILogger<KafkaConsumerService<TMessage, THandler>>>(),
            topic));

        return services;
    }
}
