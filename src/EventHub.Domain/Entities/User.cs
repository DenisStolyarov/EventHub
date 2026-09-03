using EventHub.Domain.Enums;
using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }

    public UserRole Role { get; private set; }

    public string Login
    {
        get;
        private set => field = string.IsNullOrWhiteSpace(value)
            ? throw new ValidationException(nameof(Login), "Login cannot be empty.")
            : value.Trim();
    } = null!;

    public string Password
    {
        get;
        private set => field = string.IsNullOrWhiteSpace(value)
            ? throw new ValidationException(nameof(Password), "Password cannot be empty.")
            : value;
    } = null!;

    private User() { }

    public User(Guid id, string login, string password, UserRole role)
    {
        Id = id;
        Login = login;
        Password = password;
        Role = role;
    }
}
