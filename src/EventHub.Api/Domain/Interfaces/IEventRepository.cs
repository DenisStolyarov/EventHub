using EventHub.Api.Domain.Common;
using EventHub.Api.Domain.Filters;
using EventHub.Domain.Entities;

namespace EventHub.Api.Domain.Interfaces;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<Event>> GetFilteredAsync(EventFilter filter, CancellationToken cancellationToken = default);

    void Add(Event @event);

    void Delete(Event @event);
}