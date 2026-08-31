using EventHub.Application.Abstractions.Identity;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Dtos.User;
using EventHub.Domain.Entities;
using EventHub.Domain.Exceptions;

namespace EventHub.Application.Services;

public sealed class UserService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, ITokenGenerator tokenGenerator) : IUserService
{
    public async Task<TokenDto> Login(LoginUserDto dto, CancellationToken cancellationToken = default)
    {
        User? user = await unitOfWork.Users.GetByLoginAsync(dto.Login, cancellationToken);

        if (user is null || !passwordHasher.Verify(dto.Password, user.Password))
        {
            throw new ValidationException("");
        }

        string token = tokenGenerator.GenerateToken(user);

        return new TokenDto { Token = token };
    }

    public Task<TokenDto> Register(RegisterUserDto dto, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
