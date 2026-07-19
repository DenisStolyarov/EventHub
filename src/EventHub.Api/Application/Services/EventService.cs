using EventHub.Api.Application.Constants;
using EventHub.Api.Application.Dto;
using EventHub.Api.Application.Dto.Events;
using EventHub.Api.Application.Errors;
using EventHub.Api.Application.Exceptions;
using EventHub.Api.Application.Interfaces;
using EventHub.Api.Domain.Entities;
using EventHub.Api.Domain.Exceptions;
using EventHub.Api.Domain.ValueObjects;
using EventHub.Api.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Api.Application.Services;

public sealed class EventService(AppDbContext context) : IEventService
{
    public async Task<PaginatedResult<EventDto>> GetAllAsync(GetEventsDto dto)
    {
        DateTime? from = dto.From?.UtcDateTime;
        DateTime? to = dto.To?.UtcDateTime;

        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            throw new ValidationException(nameof(GetEventsDto.From), EventServiceErrors.FromMustBeBeforeTo);
        }

        IQueryable<Event> query = context.Events;

        if (!string.IsNullOrWhiteSpace(dto.Title))
        {
            query = query.Where(e => e.Title.Contains(dto.Title));
        }
        if (from.HasValue)
        {
            query = query.Where(e => e.StartAt >= from.Value);
        }
        if (to.HasValue)
        {
            query = query.Where(e => e.EndAt <= to.Value);
        }

        int pageNumber = Math.Max(1, dto.Page);
        int pageSize = Math.Clamp(dto.PageSize, 1, Pagination.MaxPageSize);

        int totalRecords = await query.CountAsync();

        int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

        List<Event> events = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        List<EventDto> data = [.. events.ToDto()];

        return new PaginatedResult<EventDto>
        {
            Data = data,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            ItemsOnPage = data.Count,
        };
    }

    public async Task<EventDto> GetByIdAsync(Guid id)
    {
        Event? @event = await context.Events.FindAsync(id)
            ?? throw new NotFoundException(nameof(Event), id);

        return @event.ToDto();
    }

    public async Task<EventDto> CreateAsync(CreateEventDto dto)
    {
        Period period = new(dto.StartAt.UtcDateTime, dto.EndAt.UtcDateTime);
        Event @event = new(Guid.CreateVersion7(), dto.Title, dto.Description, dto.TotalSeats, period);

        context.Events.Add(@event);

        await context.SaveChangesAsync();

        return @event.ToDto();
    }

    public async Task<EventDto> UpdateAsync(Guid id, UpdateEventDto dto)
    {
        Period period = new(dto.StartAt.UtcDateTime, dto.EndAt.UtcDateTime);

        Event? existing = await context.Events.FindAsync(id)
            ?? throw new NotFoundException(nameof(Event), id);

        existing.Update(dto.Title, dto.Description, period);

        await context.SaveChangesAsync();

        return existing.ToDto();
    }

    public async Task DeleteAsync(Guid id)
    {
        Event? existing = await context.Events.FindAsync(id)
            ?? throw new NotFoundException(nameof(Event), id);

        context.Events.Remove(existing);

        await context.SaveChangesAsync();
    }
}
