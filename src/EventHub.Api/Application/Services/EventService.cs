using EventHub.Api.Application.Dto.Events;
using EventHub.Api.Application.Errors;
using EventHub.Api.Application.Exceptions;
using EventHub.Api.Application.Interfaces;
using EventHub.Api.Domain.Entities;
using EventHub.Api.Domain.Filters;
using EventHub.Api.Domain.Interfaces;
using EventHub.Api.Domain.ValueObjects;

namespace EventHub.Api.Application.Services;

public class EventService(IEventRepository repository) : IEventService
{
    public IEnumerable<EventDto> GetAll(GetEventsDto dto)
    {
        DateTime? from = dto.From?.UtcDateTime;
        DateTime? to = dto.To?.UtcDateTime;

        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            throw new ValidationException(nameof(GetEventsDto.From), EventServiceErrors.FromMustBeBeforeTo);
        }

        EventFilter filter = new() { Title = dto.Title, From = from, To = to };

        return repository.GetAll(filter).ToDto();
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