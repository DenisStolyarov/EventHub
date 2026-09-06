using EventHub.Application.Abstractions.Identity;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Persistence.Repositories;
using EventHub.Application.Dtos.User;
using EventHub.Application.Exceptions;
using EventHub.Application.Services;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using FluentAssertions;
using Moq;

namespace EventHub.UnitTests.Services;

public sealed class UserServiceTests
{
    private const string Login = "testuser";
    private const string RawPassword = "raw_password";
    private const string WrongPassword = "wrong_password";
    private const string HashedPassword = "hashed_password";
    private const string StoredHash = "stored_hash";
    private const string Token = "test-token";

    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<ITokenGenerator> _tokenGeneratorMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _unitOfWorkMock.SetupGet(u => u.Users).Returns(_userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _userService = new UserService(_unitOfWorkMock.Object, _passwordHasherMock.Object, _tokenGeneratorMock.Object);
    }

    [Fact]
    public async Task Register_ValidDto_PersistsUserWithHashedPassword()
    {
        // Arrange
        RegisterUserDto dto = new() { Login = Login, Password = RawPassword, Role = UserRole.User };

        _userRepoMock
            .Setup(r => r.ExistsByLoginAsync(Login, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(p => p.Hash(RawPassword))
            .Returns(HashedPassword);

        // Act
        await _userService.Register(dto, TestContext.Current.CancellationToken);

        // Assert
        _userRepoMock.Verify(r => r.Add(It.Is<User>(u => u.Password == HashedPassword && u.Password != RawPassword)), Times.Once);
        _userRepoMock.Verify(r => r.ExistsByLoginAsync(Login, It.IsAny<CancellationToken>()), Times.Once);
        _passwordHasherMock.Verify(p => p.Hash(RawPassword), Times.Once);
    }

    [Fact]
    public async Task Register_DuplicateLogin_ThrowsUserAlreadyExistsException()
    {
        // Arrange
        RegisterUserDto dto = new() { Login = Login, Password = RawPassword, Role = UserRole.User };

        _userRepoMock
            .Setup(r => r.ExistsByLoginAsync(Login, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = () => _userService.Register(dto, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UserAlreadyExistsException>();

        _userRepoMock.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
        _passwordHasherMock.Verify(p => p.Hash(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Register_DefaultRoleIsUser_WhenRoleNotSpecified()
    {
        // Arrange
        RegisterUserDto dto = new() { Login = Login, Password = RawPassword, Role = null };

        _userRepoMock
            .Setup(r => r.ExistsByLoginAsync(Login, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(p => p.Hash(RawPassword))
            .Returns(HashedPassword);

        // Act
        await _userService.Register(dto, TestContext.Current.CancellationToken);

        // Assert
        _userRepoMock.Verify(r => r.Add(It.Is<User>(u => u.Role == UserRole.User)), Times.Once);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        // Arrange
        LoginUserDto dto = new() { Login = Login, Password = RawPassword };
        User user = new(Guid.CreateVersion7(), Login, StoredHash, UserRole.User);

        _userRepoMock
            .Setup(r => r.GetByLoginAsync(Login, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(p => p.Verify(RawPassword, StoredHash))
            .Returns(true);

        _tokenGeneratorMock
            .Setup(t => t.GenerateToken(user))
            .Returns(Token);

        // Act
        TokenDto result = await _userService.Login(dto, TestContext.Current.CancellationToken);

        // Assert
        result.Token.Should().Be(Token);

        _tokenGeneratorMock.Verify(t => t.GenerateToken(user), Times.Once);
    }

    [Fact]
    public async Task Login_InvalidPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        LoginUserDto dto = new() { Login = Login, Password = WrongPassword };
        User user = new(Guid.CreateVersion7(), Login, StoredHash, UserRole.User);

        _userRepoMock
            .Setup(r => r.GetByLoginAsync(Login, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(p => p.Verify(WrongPassword, StoredHash))
            .Returns(false);

        // Act
        Func<Task> act = () => _userService.Login(dto, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();

        _tokenGeneratorMock.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Login_UserNotFound_ThrowsUnauthorizedException()
    {
        // Arrange
        LoginUserDto dto = new() { Login = Login, Password = RawPassword };

        _userRepoMock
            .Setup(r => r.GetByLoginAsync(Login, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        Func<Task> act = () => _userService.Login(dto, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();

        _tokenGeneratorMock.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
    }
}
