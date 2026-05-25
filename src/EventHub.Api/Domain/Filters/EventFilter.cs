namespace EventHub.Api.Domain.Filters;

public sealed record EventFilter
{
    public string? Title { get; init; }

    public DateTime? From { get; init; }
    
    public DateTime? To { get; init; }
}