using EventHub.Api.Domain.Exceptions;
using EventHub.Api.Domain.ValueObjects;

namespace EventHub.Api.Domain.Entities;

public class Event
{
    public Guid Id { get; }

    public string? Description { get; private set; }

    public DateTime StartAt { get; private set; }

    public DateTime EndAt { get; private set; }

    public int AvailableSeats { get; private set; }

    public List<Booking> Bookings { get; private set; } = [];

    public string Title
    {
        get;
        private set => field = string.IsNullOrWhiteSpace(value)
            ? throw new ValidationException(nameof(Title), "Title cannot be empty.")
            : value.Trim();
    } = null!;

    public int TotalSeats
    {
        get;
        private set => field = value <= 0
            ? throw new ValidationException(nameof(TotalSeats), "Total seats must be greater than zero.")
            : value;
    }

    private Event() { }

    public Event(Guid id, string title, string? description, int totalSeats, Period period)
    {
        Id = id;
        Title = title;
        Description = description;
        TotalSeats = totalSeats;
        AvailableSeats = totalSeats;
        StartAt = period.StartAt;
        EndAt = period.EndAt;
    }

    public void Update(string title, string? description, Period period)
    {
        Title = title;
        Description = description;
        StartAt = period.StartAt;
        EndAt = period.EndAt;
    }

    public bool TryReserveSeats(int count = 1)
    {
        if (count <= 0 || count > AvailableSeats)
        {
            return false;
        }

        AvailableSeats -= count;

        return true;
    }

    public void ReleaseSeats(int count = 1)
    {
        if (count <= 0 || AvailableSeats + count > TotalSeats)
        {
            throw new DomainException(nameof(AvailableSeats), "Cannot release more seats than available.");
        }

        AvailableSeats += count;
    }
}