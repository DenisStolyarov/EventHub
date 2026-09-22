namespace EventHub.Shared.Messaging;

public interface IOutboxProcessor
{
    Task<IReadOnlyCollection<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default);

    Task MarkProcessedAsync(IReadOnlyCollection<OutboxMessage> messages, DateTime processedAt, CancellationToken cancellationToken = default);
}
