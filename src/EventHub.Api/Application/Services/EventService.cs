using EventHub.Api.Application.Dto.Events;
using EventHub.Api.Application.Exceptions;
using EventHub.Api.Application.Interfaces;
using EventHub.Api.Domain.Entities;
using EventHub.Api.Domain.Interfaces;
using EventHub.Api.Domain.ValueObjects;

namespace EventHub.Api.Application.Services;

public class EventService(IEventRepository repository) : IEventService
{
    public IEnumerable<EventDto> GetAll()
    {
        IEnumerable<Event> events = repository.GetAll();

        return events.ToDto();
    }

    public EventDto GetById(Guid id)
    {
        Event? @event = repository.GetById(id)
            ?? throw new NotFoundException(nameof(Event), id);

        return @event.ToDto();
    }

    public EventDto Create(CreateEventDto dto)
    {
        Period period = new(dto.StartAt.UtcDateTime, dto.EndAt.UtcDateTime);
        Event @event = new(Guid.CreateVersion7(), dto.Title, dto.Description, period);

        repository.Add(@event);

        return @event.ToDto();
    }

    public EventDto Update(Guid id, UpdateEventDto dto)
    {
        Event? existing = repository.GetById(id)
            ?? throw new NotFoundException(nameof(Event), id);

        Period period = new(dto.StartAt.UtcDateTime, dto.EndAt.UtcDateTime);
        Event updated = new(existing.Id, dto.Title, dto.Description, period);

        repository.Update(updated);

        return updated.ToDto();
    }

    public void Delete(Guid id)
    {
        Event? existing = repository.GetById(id)
            ?? throw new NotFoundException(nameof(Event), id);

        repository.Delete(id);
    }
}