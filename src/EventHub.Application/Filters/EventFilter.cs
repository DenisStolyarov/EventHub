namespace EventHub.Application.Filters;

public sealed record EventFilter
{
    public string? Title { get; init; }

    public DateTime? From { get; init; }

    public DateTime? To { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }
}