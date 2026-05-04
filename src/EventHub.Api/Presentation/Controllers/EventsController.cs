using Asp.Versioning;
using EventHub.Api.Application.Dto.Events;
using EventHub.Api.Application.Interfaces;
using EventHub.Api.Presentation.Dto.Events;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/events")]
public class EventsController(IEventService eventService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<EventResponse>> GetAll()
    {
        IEnumerable<EventDto> events = eventService.GetAll();
        IEnumerable<EventResponse> response = events.Select(MapToResponse);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    public ActionResult<EventResponse> GetById(Guid id)
    {
        EventDto? @event = eventService.GetById(id);

        if (@event is null)
        {
            return NotFound();
        }

        EventResponse response = MapToResponse(@event);

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    public ActionResult<EventResponse> Create(CreateEventRequest request)
    {
        if (request.EndAt <= request.StartAt)
        {
            ModelState.AddModelError(nameof(request.EndAt), "EndAt must be later than StartAt.");

            return ValidationProblem(ModelState);
        }

        CreateEventDto dto = new()
        {
            Title = request.Title,
            Description = request.Description,
            StartAt = request.StartAt,
            EndAt = request.EndAt
        };

        EventDto created = eventService.Create(dto);
        EventResponse response = MapToResponse(created);

        return CreatedAtAction(nameof(GetById), new { id = created.Id, version = "1.0" }, response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    public ActionResult<EventResponse> Update(Guid id, UpdateEventRequest request)
    {
        if (request.EndAt <= request.StartAt)
        {
            ModelState.AddModelError(nameof(request.EndAt), "EndAt must be later than StartAt.");

            return ValidationProblem(ModelState);
        }

        UpdateEventDto dto = new()
        {
            Title = request.Title,
            Description = request.Description,
            StartAt = request.StartAt,
            EndAt = request.EndAt
        };

        EventDto? updated = eventService.Update(id, dto);

        if (updated is null)
        {
            return NotFound();
        }

        EventResponse response = MapToResponse(updated);

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(Guid id)
    {
        bool deleted = eventService.Delete(id);

        return deleted
            ? NoContent()
            : NotFound();
    }

    private static EventResponse MapToResponse(EventDto dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title,
        Description = dto.Description,
        StartAt = dto.StartAt,
        EndAt = dto.EndAt
    };
}