using EventHub.Api.Application.Dto.Events;

namespace EventHub.Api.Presentation.Dto.Events;

public static class EventMappingExtensions
{
    public static EventResponse ToResponse(this EventDto dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title,
        Description = dto.Description,
        StartAt = dto.StartAt,
        EndAt = dto.EndAt
    };

    public static IEnumerable<EventResponse> ToResponse(this IEnumerable<EventDto> dtos) =>
        dtos.Select(ToResponse);
}
