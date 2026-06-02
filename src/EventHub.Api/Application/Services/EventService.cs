using EventHub.Api.Application.Constants;
using EventHub.Api.Application.Dto;
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
    public PaginatedResult<EventDto> GetAll(GetEventsDto dto)
    {
        DateTime? from = dto.From?.UtcDateTime;
        DateTime? to = dto.To?.UtcDateTime;

        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            throw new ValidationException(nameof(GetEventsDto.From), EventServiceErrors.FromMustBeBeforeTo);
        }

        EventFilter filter = new() { Title = dto.Title, From = from, To = to };

        int pageNumber = Math.Max(1, dto.Page);
        int pageSize = Math.Clamp(dto.PageSize, 1, Pagination.MaxPageSize);

        int totalRecords = repository.Count(filter);
        int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

        IEnumerable<EventDto> data = repository
            .GetAll(filter, pageNumber, pageSize)
            .ToDto();

        return new PaginatedResult<EventDto>
        {
            Data = data,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            ItemsOnPage = data.Count(),
        };
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
        Period period = new(dto.StartAt.UtcDateTime, dto.EndAt.UtcDateTime);

        Event? existing = repository.GetById(id)
            ?? throw new NotFoundException(nameof(Event), id);

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