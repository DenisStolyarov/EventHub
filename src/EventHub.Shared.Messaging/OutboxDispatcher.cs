using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventHub.Shared.Messaging;

public sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    KafkaProducer producer,
    TimeProvider timeProvider,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    private const int BatchSize = 20;

    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox dispatcher is started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        logger.LogInformation("Outbox dispatcher is stopped");
    }

    private async Task DispatchPendingMessagesAsync(CancellationToken stoppingToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

        IOutboxProcessor outbox = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

        IReadOnlyCollection<OutboxMessage> messages = await outbox.GetPendingAsync(BatchSize, stoppingToken);

        if (messages.Count == 0)
        {
            return;
        }

        foreach (OutboxMessage message in messages)
        {
            await producer.ProduceAsync(message.Topic, message.Key, message.Payload, stoppingToken);
        }

        DateTime processedAt = timeProvider.GetUtcNow().UtcDateTime;

        await outbox.MarkProcessedAsync(messages, processedAt, stoppingToken);
    }
}
