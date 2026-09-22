namespace EventHub.Shared.Messaging;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    public string Topic { get; set; } = null!;

    public string Key { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public DateTime OccurredAt { get; set; }

    public DateTime? ProcessedAt { get; set; }
}
