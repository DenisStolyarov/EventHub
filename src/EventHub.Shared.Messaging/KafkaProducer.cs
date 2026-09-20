using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace EventHub.Shared.Messaging;

public sealed class KafkaProducer(IOptions<MessagingOptions> options) : IDisposable
{
    private readonly IProducer<string, string> _producer =
        new ProducerBuilder<string, string>(
                new ProducerConfig { BootstrapServers = options.Value.BootstrapServers })
            .Build();

    public Task ProduceAsync(string topic, string key, string payload, CancellationToken cancellationToken = default) =>
        _producer.ProduceAsync(
            topic,
            new Message<string, string> { Key = key, Value = payload },
            cancellationToken);

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));

        _producer.Dispose();
    }
}