using EventHub.Api.Domain.Entities;
using EventHub.Api.Domain.Filters;
using EventHub.Api.Domain.Interfaces;

namespace EventHub.Api.Infrastructure.Repositories;

public class InMemoryEventRepository : IEventRepository
{
    private readonly Lock _lock = new();
    private readonly List<Event> _events = [];

    public IEnumerable<Event> GetAll(EventFilter filter)
    {
        lock (_lock)
        {
            IEnumerable<Event> query = _events;

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

            return [.. query];
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
}