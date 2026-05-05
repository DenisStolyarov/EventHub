namespace EventHub.Api.Application.Dto.Events;

public sealed record CreateEventDto
{
    public required string Title { get; init; }

    public string? Description { get; init; }

    public required DateTimeOffset StartAt { get; init; }

    public required DateTimeOffset EndAt { get; init; }
}