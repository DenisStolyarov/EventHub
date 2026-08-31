using EventHub.Application.Dtos.User;

namespace EventHub.Application.Abstractions.Services;

public interface IUserService
{
    Task<TokenDto> Login(LoginUserDto dto, CancellationToken cancellationToken = default);

    Task<TokenDto> Register(RegisterUserDto dto, CancellationToken cancellationToken = default);
}
