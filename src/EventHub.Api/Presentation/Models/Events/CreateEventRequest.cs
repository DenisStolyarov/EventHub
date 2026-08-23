using System.ComponentModel.DataAnnotations;

namespace EventHub.Api.Presentation.Models.Events;

public sealed record CreateEventRequest
{
    [Required]
    public required string Title { get; init; }

    public string? Description { get; init; }

    [Required]
    public required int TotalSeats { get; init; }

    [Required]
    public required DateTimeOffset StartAt { get; init; }

    [Required]
    public required DateTimeOffset EndAt { get; init; }
}
