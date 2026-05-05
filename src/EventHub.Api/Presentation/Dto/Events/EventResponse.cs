namespace EventHub.Api.Presentation.Dto.Events;

public sealed record EventResponse
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required DateTimeOffset StartAt { get; init; }

    public required DateTimeOffset EndAt { get; init; }
}