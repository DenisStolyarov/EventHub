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
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<EventDto>> GetAll()
    {
        IEnumerable<EventDto> events = eventService.GetAll();

        return Ok(events);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<EventDto> GetById(Guid id)
    {
        EventDto @event = eventService.GetById(id);

        return Ok(@event);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ActionResult<EventDto> Create(CreateEventRequest request)
    {
        CreateEventDto dto = new()
        {
            Title = request.Title,
            Description = request.Description,
            StartAt = request.StartAt,
            EndAt = request.EndAt
        };

        EventDto created = eventService.Create(dto);

        return CreatedAtAction(nameof(GetById), new { id = created.Id, version = "1.0" }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ActionResult<EventDto> Update(Guid id, UpdateEventRequest request)
    {
        UpdateEventDto dto = new()
        {
            Title = request.Title,
            Description = request.Description,
            StartAt = request.StartAt,
            EndAt = request.EndAt
        };

        EventDto updated = eventService.Update(id, dto);

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(Guid id)
    {
        eventService.Delete(id);

        return NoContent();
    }
}