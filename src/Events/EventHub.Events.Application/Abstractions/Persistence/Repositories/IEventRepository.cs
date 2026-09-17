using EventHub.Events.Application.Common;
using EventHub.Events.Application.Filters;
using EventHub.Events.Domain.Entities;

namespace EventHub.Events.Application.Abstractions.Persistence.Repositories;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<Event>> GetFilteredAsync(EventFilter filter, CancellationToken cancellationToken = default);

    void Add(Event @event);

    void Delete(Event @event);
}
