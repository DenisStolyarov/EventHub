namespace EventHub.Application.Dtos.User;

public sealed record TokenDto
{
    public required string Token { get; init; }
}
