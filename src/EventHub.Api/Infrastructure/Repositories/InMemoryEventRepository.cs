using EventHub.Api.Domain.Entities;
using EventHub.Api.Domain.Filters;
using EventHub.Api.Domain.Interfaces;

namespace EventHub.Api.Infrastructure.Repositories;

public class InMemoryEventRepository : IEventRepository
{
    private readonly Lock _lock = new();
    private readonly List<Event> _events = [];

    public int Count(EventFilter filter)
    {
        lock (_lock)
        {
            return ApplyFilter(_events, filter).Count();
        }
    }

    public IEnumerable<Event> GetAll(EventFilter filter, int pageNumber, int pageSize)
    {
        lock (_lock)
        {
            IEnumerable<Event> filteredEvents = ApplyFilter(_events, filter);
            IEnumerable<Event> pagedEvents = GetPage(filteredEvents, pageNumber, pageSize);

            return [.. pagedEvents];
        }
    }

    public Event? GetById(Guid id)
    {
        lock (_lock)
        {
            return _events.Find(e => e.Id == id);
        }
    }

    public void Add(Event @event)
    {
        lock (_lock)
        {
            _events.Add(@event);
        }
    }

    public void Update(Event @event)
    {
        lock (_lock)
        {
            int index = _events.FindIndex(e => e.Id == @event.Id);

            if (index < 0)
            {
                throw new KeyNotFoundException($"Event with id '{@event.Id}' not found.");
            }

            _events[index] = @event;
        }
    }

    public void Delete(Guid id)
    {
        lock (_lock)
        {
            int index = _events.FindIndex(e => e.Id == id);

            if (index < 0)
            {
                throw new KeyNotFoundException($"Event with id '{id}' not found.");
            }

            _events.RemoveAt(index);
        }
    }

    private static IEnumerable<Event> ApplyFilter(IEnumerable<Event> events, EventFilter filter)
    {
        IEnumerable<Event> query = events;

        if (!string.IsNullOrWhiteSpace(filter.Title))
        {
            query = query.Where(e => e.Title.Contains(filter.Title, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.From.HasValue)
        {
            query = query.Where(e => e.StartAt >= filter.From.Value);
        }

        if (filter.To.HasValue)
        {
            query = query.Where(e => e.EndAt <= filter.To.Value);
        }

        return query;
    }

    private static IEnumerable<Event> GetPage(IEnumerable<Event> events, int pageNumber, int pageSize) => events
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize);
}