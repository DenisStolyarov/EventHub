using EventHub.Application.Common;
using EventHub.Application.Filters;
using EventHub.Domain.Entities;

namespace EventHub.Application.Abstractions.Persistence.Repositories;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<Event>> GetFilteredAsync(EventFilter filter, CancellationToken cancellationToken = default);

    void Add(Event @event);

    void Delete(Event @event);
}
