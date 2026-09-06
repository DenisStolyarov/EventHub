using EventHub.Application.Dtos.User;

namespace EventHub.Api.Presentation.Models.Auth;

public static class AuthRequestMappingExtensions
{
    public static RegisterUserDto ToDto(this RegisterUserRequest request) => new()
    {
        Login = request.Login,
        Password = request.Password,
        Role = request.Role
    };

    public static LoginUserDto ToDto(this LoginUserRequest request) => new()
    {
        Login = request.Login,
        Password = request.Password
    };
}
