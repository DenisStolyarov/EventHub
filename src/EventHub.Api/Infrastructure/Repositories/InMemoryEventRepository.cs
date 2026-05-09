using EventHub.Api.Domain.Entities;
using EventHub.Api.Domain.Interfaces;

namespace EventHub.Api.Infrastructure.Repositories;

public class InMemoryEventRepository : IEventRepository
{
    private readonly Lock _lock = new();
    private readonly List<Event> _events = [];

    public IEnumerable<Event> GetAll()
    {
        lock (_lock)
        {
            return [.. _events];
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