using EventHub.Api.Application.Dto.Events;
using EventHub.Api.Application.Interfaces;
using EventHub.Api.Domain.Entities;
using EventHub.Api.Domain.Interfaces;

namespace EventHub.Api.Application.Services;

public class EventService(IEventRepository repository) : IEventService
{
    public IEnumerable<EventDto> GetAll()
    {
        IEnumerable<Event> events = repository.GetAll();

        return events.Select(MapToDto);
    }

    public EventDto? GetById(Guid id)
    {
        Event? @event = repository.GetById(id);

        return @event is null
            ? null
            : MapToDto(@event);
    }

    public EventDto Create(CreateEventDto dto)
    {
        Event @event = new()
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            StartAt = dto.StartAt.UtcDateTime,
            EndAt = dto.EndAt.UtcDateTime
        };

        repository.Add(@event);

        return MapToDto(@event);
    }

    public EventDto? Update(Guid id, UpdateEventDto dto)
    {
        Event? existing = repository.GetById(id);

        if (existing is null)
        {
            return null;
        }

        Event updated = new()
        {
            Id = existing.Id,
            Title = dto.Title,
            Description = dto.Description,
            StartAt = dto.StartAt.UtcDateTime,
            EndAt = dto.EndAt.UtcDateTime
        };

        repository.Update(updated);

        return MapToDto(updated);
    }

    public bool Delete(Guid id)
    {
        Event? existing = repository.GetById(id);

        if (existing is null)
        {
            return false;
        }

        repository.Delete(id);

        return true;
    }

    private static EventDto MapToDto(Event @event) => new()
    {
        Id = @event.Id,
        Title = @event.Title,
        Description = @event.Description,
        StartAt = new(@event.StartAt, TimeSpan.Zero),
        EndAt = new(@event.EndAt, TimeSpan.Zero)
    };
}