using EventHub.Domain.Entities;

namespace EventHub.Application.Dto.Events;

public static class EventMappingExtensions
{
    public static EventDto ToDto(this Event @event) => new()
    {
        Id = @event.Id,
        Title = @event.Title,
        Description = @event.Description,
        TotalSeats = @event.TotalSeats,
        AvailableSeats = @event.AvailableSeats,
        StartAt = new(@event.StartAt, TimeSpan.Zero),
        EndAt = new(@event.EndAt, TimeSpan.Zero)
    };

    public static IEnumerable<EventDto> ToDto(this IEnumerable<Event> events) =>
        events.Select(ToDto);
}