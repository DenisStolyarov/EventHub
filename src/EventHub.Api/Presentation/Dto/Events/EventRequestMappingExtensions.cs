using EventHub.Api.Application.Dto.Events;

namespace EventHub.Api.Presentation.Dto.Events;

public static class EventRequestMappingExtensions
{
    public static CreateEventDto ToDto(this CreateEventRequest request) => new()
    {
        Title = request.Title,
        Description = request.Description,
        TotalSeats = request.TotalSeats,
        StartAt = request.StartAt,
        EndAt = request.EndAt
    };

    public static UpdateEventDto ToDto(this UpdateEventRequest request) => new()
    {
        Title = request.Title,
        Description = request.Description,
        StartAt = request.StartAt,
        EndAt = request.EndAt
    };

    public static GetEventsDto ToDto(this GetEventsRequest request) => new()
    {
        Title = request.Title,
        From = request.From,
        To = request.To,
        Page = request.Page,
        PageSize = request.PageSize
    };
}