using Asp.Versioning;
using EventHub.Api.Presentation.Models.Auth;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Dtos.User;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController(IUserService userService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        RegisterUserDto dto = request.ToDto();

        await userService.Register(dto, cancellationToken);

        return NoContent();
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenDto>> Login(LoginUserRequest request, CancellationToken cancellationToken)
    {
        LoginUserDto dto = request.ToDto();

        TokenDto token = await userService.Login(dto, cancellationToken);

        return Ok(token);
    }
}
