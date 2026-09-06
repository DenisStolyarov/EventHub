using System.ComponentModel.DataAnnotations;
using EventHub.Domain.Enums;

namespace EventHub.Api.Presentation.Models.Auth;

public sealed record RegisterUserRequest : IValidatableObject
{
    [Required(AllowEmptyStrings = false)]
    public required string Login { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string Password { get; init; }

    public UserRole? Role { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Role is { } role && role == UserRole.Unassigned)
        {
            yield return new ValidationResult("Role cannot be Unassigned.", [nameof(Role)]);
        }
    }
}
