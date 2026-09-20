using System.ComponentModel.DataAnnotations;

namespace EventHub.Shared.Messaging;

public sealed class MessagingOptions
{
    public const string SectionName = "Kafka";

    [Required]
    public string BootstrapServers { get; init; } = string.Empty;

    [Required]
    public string ConsumerGroup { get; init; } = string.Empty;
}
