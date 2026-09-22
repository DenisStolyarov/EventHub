using EventHub.Users.Application.Dtos.User;

namespace EventHub.Users.Application.Abstractions.Services;

public interface IUserService
{
    Task<TokenDto> Login(LoginUserDto dto, CancellationToken cancellationToken = default);

    Task Register(RegisterUserDto dto, CancellationToken cancellationToken = default);
}
