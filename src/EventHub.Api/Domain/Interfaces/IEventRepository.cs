using EventHub.Api.Domain.Entities;

namespace EventHub.Api.Domain.Interfaces;

public interface IEventRepository
{
    IEnumerable<Event> GetAll();

    Event? GetById(Guid id);

    void Add(Event @event);

    void Update(Event @event);

    void Delete(Guid id);
}