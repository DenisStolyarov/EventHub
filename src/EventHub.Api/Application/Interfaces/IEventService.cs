using EventHub.Api.Application.Dto;
using EventHub.Api.Application.Dto.Events;

namespace EventHub.Api.Application.Interfaces;

public interface IEventService
{
    Task<PaginatedResult<EventDto>> GetAllAsync(GetEventsDto dto, CancellationToken cancellationToken = default);

    Task<EventDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<EventDto> CreateAsync(CreateEventDto dto, CancellationToken cancellationToken = default);

    Task<EventDto> UpdateAsync(Guid id, UpdateEventDto dto, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}