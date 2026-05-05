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

        return events.ToDto();
    }

    public EventDto? GetById(Guid id)
    {
        Event? @event = repository.GetById(id);

        return @event?.ToDto();
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

        return @event.ToDto();
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

        return updated.ToDto();
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
}