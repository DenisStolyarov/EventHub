using Asp.Versioning;
using EventHub.Api.Application.Dto.Events;
using EventHub.Api.Application.Interfaces;
using EventHub.Api.Presentation.Dto.Events;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("events")]
public class EventsController(IEventService eventService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<EventResponse>> GetAll()
    {
        IEnumerable<EventDto> events = eventService.GetAll();

        return Ok(events.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    public ActionResult<EventResponse> GetById(Guid id)
    {
        EventDto? @event = eventService.GetById(id);

        return @event is null
            ? NotFound()
            : Ok(@event.ToResponse());
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    public ActionResult<EventResponse> Create(CreateEventRequest request)
    {
        CreateEventDto dto = new()
        {
            Title = request.Title,
            Description = request.Description,
            StartAt = request.StartAt,
            EndAt = request.EndAt
        };

        try
        {
            EventDto created = eventService.Create(dto);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created.ToResponse());
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(ex.ParamName ?? string.Empty, ex.Message);

            return ValidationProblem(ModelState);
        }        
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    public ActionResult<EventResponse> Update(Guid id, UpdateEventRequest request)
    {
        UpdateEventDto dto = new()
        {
            Title = request.Title,
            Description = request.Description,
            StartAt = request.StartAt,
            EndAt = request.EndAt
        };
        
        try
        {
            EventDto? updated = eventService.Update(id, dto);

            return updated is null
                ? NotFound()
                : Ok(updated.ToResponse());
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(ex.ParamName ?? string.Empty, ex.Message);

            return ValidationProblem(ModelState);
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(Guid id)
    {
        bool deleted = eventService.Delete(id);

        return deleted ? NoContent() : NotFound();
    }
}