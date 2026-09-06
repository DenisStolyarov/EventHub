namespace EventHub.Application.Dtos.User;

public sealed record LoginUserDto
{
    public required string Login { get; init; }

    public required string Password { get; init; }
}
