using EventHub.Users.Domain.Enums;

namespace EventHub.Users.Application.Dtos.User;

public sealed record RegisterUserDto
{
    public required string Login { get; init; }

    public required string Password { get; init; }

    public UserRole? Role { get; init; }
}
