namespace EventHub.Users.Application.Abstractions.Identity;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}
