using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventHub.Shared.Messaging;

public sealed class KafkaTopicInitializer(
    IOptions<MessagingOptions> options,
    ILogger<KafkaTopicInitializer> logger,
    params string[] topics) : BackgroundService
{
    private const int MaxAttempts = 10;

    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await CreateMissingTopicsAsync();

                return;
            }
            catch (CreateTopicsException ex) when (ex.Results.All(r => r.Error.Code is ErrorCode.NoError or ErrorCode.TopicAlreadyExists))
            {
                logger.LogInformation("Kafka topics are ready, some of them were created concurrently: {Topics}", string.Join(", ", topics));

                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Attempt {Attempt} to create Kafka topics failed: {Message}", attempt, ex.Message);
            }

            await Task.Delay(RetryDelay, stoppingToken);
        }

        logger.LogError("Kafka topics were not created after {MaxAttempts} attempts, skipping", MaxAttempts);
    }

    private async Task CreateMissingTopicsAsync()
    {
        using IAdminClient adminClient =
            new AdminClientBuilder(new AdminClientConfig { BootstrapServers = options.Value.BootstrapServers })
                .Build();

        HashSet<string> existingTopics = [.. adminClient
            .GetMetadata(TimeSpan.FromSeconds(5))
            .Topics
            .Select(t => t.Topic)];

        HashSet<TopicSpecification> missingTopics = [.. topics
            .Where(t => !existingTopics.Contains(t))
            .Select(t => new TopicSpecification { Name = t, NumPartitions = 1, ReplicationFactor = 1 })];

        if (missingTopics.Count == 0)
        {
            logger.LogInformation("All Kafka topics already exist");

            return;
        }

        await adminClient.CreateTopicsAsync(missingTopics);

        logger.LogInformation("Kafka topics were created: {Topics}", string.Join(", ", missingTopics.Select(t => t.Name)));
    }
}
