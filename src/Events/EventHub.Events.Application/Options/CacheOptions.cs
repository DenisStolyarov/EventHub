using System.ComponentModel.DataAnnotations;

namespace EventHub.Events.Application.Options;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    [Range(1, 86400)]
    public int EventTtlSeconds { get; init; } = 600;

    [Range(1, 86400)]
    public int TopTtlSeconds { get; init; } = 60;
}
