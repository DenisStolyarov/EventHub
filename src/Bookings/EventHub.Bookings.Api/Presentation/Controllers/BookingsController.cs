using Asp.Versioning;
using EventHub.Bookings.Api.Presentation.Models.Bookings;
using EventHub.Bookings.Application.Abstractions.Services;
using EventHub.Bookings.Application.Dtos.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Bookings.Api.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/bookings")]
[Authorize]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(BookingInfo), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingInfo>> Create(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        BookingInfo booking = await bookingService.CreateBookingAsync(request.EventId, cancellationToken);

        return AcceptedAtAction(nameof(GetById), new { id = booking.Id, version = "1.0" }, booking);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookingInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingInfo>> GetById(Guid id, CancellationToken cancellationToken)
    {
        BookingInfo booking = await bookingService.GetBookingByIdAsync(id, cancellationToken);

        return Ok(booking);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await bookingService.CancelBookingAsync(id, cancellationToken);

        return NoContent();
    }
}
