using System.ComponentModel.DataAnnotations;

namespace EventHub.Api.Presentation.Dto.Events;

public sealed record CreateEventRequest
{
    [Required]
    public required string Title { get; init; }

    public string? Description { get; init; }

    [Required]
    public required DateTime StartAt { get; init; }

    [Required]
    public required DateTime EndAt { get; init; }
}