using EventHub.Api.Domain.Entities;
using EventHub.Api.Domain.Filters;

namespace EventHub.Api.Domain.Interfaces;

public interface IEventRepository
{
    int Count(EventFilter filter);

    IEnumerable<Event> GetAll(EventFilter filter, int page, int pageSize);

    Event? GetById(Guid id);

    void Add(Event @event);

    void Update(Event @event);

    void Delete(Guid id);
}