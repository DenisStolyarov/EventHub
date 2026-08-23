using EventHub.Application.Dtos;
using EventHub.Application.Dtos.Events;

namespace EventHub.Application.Abstractions.Services;

public interface IEventService
{
    Task<PaginatedResult<EventDto>> GetAllAsync(GetEventsDto dto, CancellationToken cancellationToken = default);

    Task<EventDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<EventDto> CreateAsync(CreateEventDto dto, CancellationToken cancellationToken = default);

    Task<EventDto> UpdateAsync(Guid id, UpdateEventDto dto, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
