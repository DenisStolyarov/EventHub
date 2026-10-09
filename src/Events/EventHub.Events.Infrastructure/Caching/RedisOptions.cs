using System.ComponentModel.DataAnnotations;

namespace EventHub.Events.Infrastructure.Caching;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    [Required(AllowEmptyStrings = false)]
    public required string Host { get; init; }

    [Range(1, 65535)]
    public int Port { get; init; }

    public string? Password { get; init; }

    [Range(1, 60000)]
    public int ConnectTimeout { get; init; } = 5000;

    [Range(1, 60000)]
    public int SyncTimeout { get; init; } = 3000;

    [Range(0, 10)]
    public int ConnectRetry { get; init; } = 3;

    public bool AbortOnConnectFail { get; init; } = false;
}
