using EventHub.Domain.Enums;

namespace EventHub.Application.Dtos.User;

public sealed record RegisterUserDto
{
    public required string Login { get; init; }

    public required string Password { get; init; }

    public UserRole? Role { get; init; }
}
