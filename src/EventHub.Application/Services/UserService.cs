using EventHub.Application.Abstractions.Identity;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Dtos.User;
using EventHub.Application.Errors;
using EventHub.Application.Exceptions;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;

namespace EventHub.Application.Services;

public sealed class UserService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, ITokenGenerator tokenGenerator) : IUserService
{
    public async Task<TokenDto> Login(LoginUserDto dto, CancellationToken cancellationToken = default)
    {
        User? user = await unitOfWork.Users.GetByLoginAsync(dto.Login, cancellationToken);

        if (user is null || !passwordHasher.Verify(dto.Password, user.Password))
        {
            throw new UnauthorizedException(UserServiceErrors.InvalidCredentials);
        }

        string token = tokenGenerator.GenerateToken(user);

        return new TokenDto { Token = token };
    }

    public async Task Register(RegisterUserDto dto, CancellationToken cancellationToken = default)
    {
        bool userExists = await unitOfWork.Users.ExistsByLoginAsync(dto.Login, cancellationToken);

        if (userExists)
        {
            throw new UserAlreadyExistsException();
        }

        UserRole role = dto.Role ?? UserRole.User;

        string passwordHash = passwordHasher.Hash(dto.Password);

        User user = new(Guid.CreateVersion7(), dto.Login, passwordHash, role);

        unitOfWork.Users.Add(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
