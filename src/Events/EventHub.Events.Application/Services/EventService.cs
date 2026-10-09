using EventHub.Events.Application.Abstractions.Caching;
using EventHub.Events.Application.Abstractions.Persistence;
using EventHub.Events.Application.Abstractions.Services;
using EventHub.Events.Application.Common;
using EventHub.Events.Application.Constants;
using EventHub.Events.Application.Dtos;
using EventHub.Events.Application.Dtos.Events;
using EventHub.Events.Application.Errors;
using EventHub.Events.Application.Exceptions;
using EventHub.Events.Application.Filters;
using EventHub.Events.Application.Options;
using EventHub.Events.Domain.Entities;
using EventHub.Events.Domain.Exceptions;
using EventHub.Events.Domain.ValueObjects;
using EventHub.Shared.Contracts;
using Microsoft.Extensions.Options;

namespace EventHub.Events.Application.Services;

public sealed class EventService(IUnitOfWork unitOfWork, ICacheService cache, IOptions<CacheOptions> options) : IEventService
{
    private const int TopEventsCount = 10;

    private CacheOptions Options { get; } = options.Value;

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

    public async Task<IReadOnlyCollection<EventDto>> GetTopAsync(CancellationToken cancellationToken = default)
    {
        List<EventDto>? cachedEvents = await cache.GetAsync<List<EventDto>>(CacheKeys.Top10, cancellationToken);

        if (cachedEvents is not null)
        {
            return cachedEvents;
        }

        IReadOnlyCollection<Event> events = await unitOfWork.Events.GetTopBySalesPercentageAsync(TopEventsCount, cancellationToken);

        List<EventDto> data = [.. events.ToDto()];

        await cache.SetAsync(CacheKeys.Top10, data, TimeSpan.FromSeconds(Options.TopTtlSeconds), cancellationToken);

        return data;
    }

    public async Task<EventDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        string cacheKey = CacheKeys.ForEvent(id);

        EventDto? cachedEvent = await cache.GetAsync<EventDto>(cacheKey, cancellationToken);

        if (cachedEvent is not null)
        {
            return cachedEvent;
        }

        Event @event = await unitOfWork.Events.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Event), id);

        EventDto dto = @event.ToDto();

        await cache.SetAsync(cacheKey, dto, TimeSpan.FromSeconds(Options.EventTtlSeconds), cancellationToken);

        return dto;
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

        await cache.RemoveAsync(CacheKeys.ForEvent(id), cancellationToken);

        return existing.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Event existing = await unitOfWork.Events.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Event), id);

        unitOfWork.Events.Delete(existing);

        EventCancelled eventCancelled = new(Guid.CreateVersion7(), id);

        unitOfWork.Outbox.Add(eventCancelled);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync(CacheKeys.ForEvent(id), cancellationToken);
    }
}
