namespace EventHub.Shared.Messaging;

public sealed class InboxMessage
{
    public Guid Id { get; set; }

    public DateTime ReceivedAt { get; set; }
}