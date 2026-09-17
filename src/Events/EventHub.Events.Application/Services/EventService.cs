using EventHub.Events.Application.Abstractions.Persistence;
using EventHub.Events.Application.Abstractions.Services;
using EventHub.Events.Application.Common;
using EventHub.Events.Application.Constants;
using EventHub.Events.Application.Dtos;
using EventHub.Events.Application.Dtos.Events;
using EventHub.Events.Application.Errors;
using EventHub.Events.Application.Exceptions;
using EventHub.Events.Application.Filters;
using EventHub.Events.Domain.Entities;
using EventHub.Events.Domain.Exceptions;
using EventHub.Events.Domain.ValueObjects;

namespace EventHub.Events.Application.Services;

public sealed class EventService(IUnitOfWork unitOfWork) : IEventService
{
    public async Task<PaginatedResult<EventDto>> GetAllAsync(GetEventsDto dto, CancellationToken cancellationToken = default)
    {
        DateTime? from = dto.From?.UtcDateTime;
        DateTime? to = dto.To?.UtcDateTime;

        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            throw new ValidationException(nameof(GetEventsDto.From), EventServiceErrors.FromMustBeBeforeTo);
        }

        int pageNumber = Math.Max(1, dto.Page);
        int pageSize = Math.Clamp(dto.PageSize, 1, Pagination.MaxPageSize);

        EventFilter filter = new() { Title = dto.Title, From = from, To = to, Page = pageNumber, PageSize = pageSize };

        PagedResult<Event> result = await unitOfWork.Events.GetFilteredAsync(filter, cancellationToken);

        List<EventDto> data = [.. result.Items.ToDto()];

        return new PaginatedResult<EventDto>
        {
            Data = data,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = result.TotalCount,
            ItemsOnPage = data.Count,
        };
    }

    public async Task<EventDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Event @event = await unitOfWork.Events.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Event), id);

        return @event.ToDto();
    }

    public async Task<EventDto> CreateAsync(CreateEventDto dto, CancellationToken cancellationToken = default)
    {
        Period period = new(dto.StartAt.UtcDateTime, dto.EndAt.UtcDateTime);
        Event @event = new(Guid.CreateVersion7(), dto.Title, dto.Description, dto.TotalSeats, period);

        unitOfWork.Events.Add(@event);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return @event.ToDto();
    }

    public async Task<EventDto> UpdateAsync(Guid id, UpdateEventDto dto, CancellationToken cancellationToken = default)
    {
        Period period = new(dto.StartAt.UtcDateTime, dto.EndAt.UtcDateTime);

        Event existing = await unitOfWork.Events.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Event), id);

        existing.Update(dto.Title, dto.Description, period);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return existing.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Event existing = await unitOfWork.Events.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Event), id);

        unitOfWork.Events.Delete(existing);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
