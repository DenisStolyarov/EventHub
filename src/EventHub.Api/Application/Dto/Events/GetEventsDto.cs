namespace EventHub.Api.Application.Dto.Events;

public sealed record GetEventsDto
{
    public string? Title { get; init; }

    public DateTimeOffset? From { get; init; }
    
    public DateTimeOffset? To { get; init; }
}