using Asp.Versioning;
using EventHub.Api.Application.Dto.Bookings;
using EventHub.Api.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/bookings")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookingInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingInfo>> GetById(Guid id)
    {
        BookingInfo booking = await bookingService.GetBookingByIdAsync(id);

        return Ok(booking);
    }
}