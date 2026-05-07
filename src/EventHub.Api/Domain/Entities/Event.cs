using EventHub.Api.Domain.ValueObjects;

namespace EventHub.Api.Domain.Entities;

public class Event
{
    public Guid Id { get; }

    public string? Description { get; }

    public DateTime StartAt { get; }

    public DateTime EndAt { get; }

    public string Title
    {
        get;
        private set => field = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Title cannot be empty.", nameof(Title))
            : value.Trim();
    }

    public Event(Guid id, string title, string? description, Period period)
    {
        Id = id;
        Title = title;
        Description = description;
        StartAt = period.StartAt;
        EndAt = period.EndAt;
    }
}