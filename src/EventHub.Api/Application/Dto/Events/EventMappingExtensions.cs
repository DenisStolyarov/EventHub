using EventHub.Api.Domain.Entities;

namespace EventHub.Api.Application.Dto.Events;

public static class EventMappingExtensions
{
    public static EventDto ToDto(this Event @event) => new()
    {
        Id = @event.Id,
        Title = @event.Title,
        Description = @event.Description,
        StartAt = new(@event.StartAt, TimeSpan.Zero),
        EndAt = new(@event.EndAt, TimeSpan.Zero)
    };

    public static IEnumerable<EventDto> ToDto(this IEnumerable<Event> events) =>
        events.Select(ToDto);
}