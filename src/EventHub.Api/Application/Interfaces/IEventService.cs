using EventHub.Api.Application.Dto;
using EventHub.Api.Application.Dto.Events;

namespace EventHub.Api.Application.Interfaces;

public interface IEventService
{
    PaginatedResult<EventDto> GetAll(GetEventsDto dto);

    EventDto GetById(Guid id);

    EventDto Create(CreateEventDto dto);

    EventDto Update(Guid id, UpdateEventDto dto);

    void Delete(Guid id);
}