using System.ComponentModel.DataAnnotations;

namespace EventHub.Api.Presentation.Models.Auth;

public sealed record LoginUserRequest
{
    [Required(AllowEmptyStrings = false)]
    public required string Login { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string Password { get; init; }
}
