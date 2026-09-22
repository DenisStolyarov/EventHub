using EventHub.Events.Application.Abstractions.Persistence.Repositories;
using EventHub.Events.Application.Common;
using EventHub.Events.Application.Filters;
using EventHub.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Events.Infrastructure.Persistence.Repositories;

public sealed class EventRepository(EventsDbContext context) : IEventRepository
{
    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Events.FindAsync([id], cancellationToken);

    public async Task<PagedResult<Event>> GetFilteredAsync(EventFilter filter, CancellationToken cancellationToken = default)
    {
        IQueryable<Event> query = context.Events;

        if (!string.IsNullOrWhiteSpace(filter.Title))
        {
            query = query.Where(e => e.Title.Contains(filter.Title));
        }

        if (filter.From.HasValue)
        {
            query = query.Where(e => e.StartAt >= filter.From.Value);
        }

        if (filter.To.HasValue)
        {
            query = query.Where(e => e.EndAt <= filter.To.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        List<Event> events = await query
            .OrderBy(e => e.StartAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Event>(events, totalCount);
    }

    public void Add(Event @event) => context.Events.Add(@event);

    public void Delete(Event @event) => context.Events.Remove(@event);
}
