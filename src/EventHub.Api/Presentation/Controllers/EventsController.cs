using Asp.Versioning;
using EventHub.Api.Application.Dto;
using EventHub.Api.Application.Dto.Bookings;
using EventHub.Api.Application.Dto.Events;
using EventHub.Api.Application.Interfaces;
using EventHub.Api.Presentation.Dto.Events;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/events")]
public class EventsController(IEventService eventService, IBookingService bookingService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResult<EventDto>>> GetAll([FromQuery] GetEventsRequest request)
    {
        GetEventsDto dto = request.ToDto();

        PaginatedResult<EventDto> page = await eventService.GetAllAsync(dto);

        return Ok(page);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> GetById(Guid id)
    {
        EventDto @event = await eventService.GetByIdAsync(id);

        return Ok(@event);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EventDto>> Create(CreateEventRequest request)
    {
        CreateEventDto dto = request.ToDto();

        EventDto created = await eventService.CreateAsync(dto);

        return CreatedAtAction(nameof(GetById), new { id = created.Id, version = "1.0" }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EventDto>> Update(Guid id, UpdateEventRequest request)
    {
        UpdateEventDto dto = request.ToDto();

        EventDto updated = await eventService.UpdateAsync(id, dto);

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await eventService.DeleteAsync(id);

        return NoContent();
    }

    [HttpPost("{id:guid}/book")]
    [ProducesResponseType(typeof(BookingInfo), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingInfo>> Book(Guid id)
    {
        BookingInfo booking = await bookingService.CreateBookingAsync(id);

        return AcceptedAtAction(
            nameof(BookingsController.GetById),
            "Bookings",
            new { id = booking.Id, version = "1.0" },
            booking);
    }
}