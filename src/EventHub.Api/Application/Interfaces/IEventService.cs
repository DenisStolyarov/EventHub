using EventHub.Api.Application.Dto;
using EventHub.Api.Application.Dto.Events;

namespace EventHub.Api.Application.Interfaces;

public interface IEventService
{
    Task<PaginatedResult<EventDto>> GetAllAsync(GetEventsDto dto);

    Task<EventDto> GetByIdAsync(Guid id);

    Task<EventDto> CreateAsync(CreateEventDto dto);

    Task<EventDto> UpdateAsync(Guid id, UpdateEventDto dto);

    Task DeleteAsync(Guid id);
}