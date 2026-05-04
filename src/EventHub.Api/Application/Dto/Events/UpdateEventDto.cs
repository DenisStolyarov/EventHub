namespace EventHub.Api.Application.Dto.Events;

public sealed record UpdateEventDto
{
    public required string Title { get; init; }

    public string? Description { get; init; }

    public required DateTime StartAt { get; init; }

    public required DateTime EndAt { get; init; }
}