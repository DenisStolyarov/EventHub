using System.Text.Json;
using Confluent.Kafka;
using EventHub.Shared.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventHub.Shared.Messaging;

public sealed class KafkaConsumerService<TMessage, THandler>(
    IServiceScopeFactory scopeFactory,
    IOptions<MessagingOptions> options,
    ILogger<KafkaConsumerService<TMessage, THandler>> logger,
    string topic)
    : BackgroundService
    where TMessage : class, IIntegrationEvent
    where THandler : class, IIntegrationMessageHandler<TMessage>
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Kafka consumer for topic {Topic} is started", topic);

        using IConsumer<string, string> consumer = CreateConsumer();

        consumer.Subscribe(topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? consumeResult = TryConsume(consumer, stoppingToken);

            if (consumeResult is null)
            {
                break;
            }

            TMessage? message = TryDeserialize(consumeResult.Message.Value);

            if (message is null)
            {
                logger.LogError("Malformed or empty message is received in topic {Topic}, skipping", topic);

                Commit(consumer, consumeResult);

                continue;
            }

            try
            {
                await HandleAsync(message, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception while handling message {Id} in topic {Topic}, skipping: {Message}", message.Id, topic, ex.Message);
            }

            Commit(consumer, consumeResult);
        }

        logger.LogInformation("Kafka consumer for topic {Topic} is stopped", topic);
    }

    private IConsumer<string, string> CreateConsumer()
    {
        ConsumerConfig config = new()
        {
            BootstrapServers = options.Value.BootstrapServers,
            GroupId = options.Value.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
        };

        return new ConsumerBuilder<string, string>(config)
            .Build();
    }

    private ConsumeResult<string, string>? TryConsume(IConsumer<string, string> consumer, CancellationToken stoppingToken)
    {
        while (true)
        {
            try
            {
                return consumer.Consume(stoppingToken);
            }
            catch (ConsumeException ex)
            {
                logger.LogWarning(ex, "Consume failed in topic {Topic}: {Message}", topic, ex.Message);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return null;
            }
        }
    }

    private static TMessage? TryDeserialize(string payload)
    {
        try
        {
            return JsonSerializer.Deserialize<TMessage>(payload);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task HandleAsync(TMessage message, CancellationToken stoppingToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

        THandler handler = scope.ServiceProvider.GetRequiredService<THandler>();

        await handler.HandleAsync(message, stoppingToken);
    }

    private void Commit(IConsumer<string, string> consumer, ConsumeResult<string, string> consumeResult)
    {
        try
        {
            consumer.Commit(consumeResult);
        }
        catch (ConsumeException ex)
        {
            logger.LogWarning(ex, "Commit failed in topic {Topic}: {Message}", topic, ex.Message);
        }
    }
}
