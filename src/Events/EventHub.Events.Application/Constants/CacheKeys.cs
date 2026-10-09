namespace EventHub.Events.Application.Constants;

public static class CacheKeys
{
    public const string EventById = "event:{0}";

    public const string Top10 = "events:top10";

    public static string ForEvent(Guid id) => string.Format(EventById, id);
}
