using EventHub.Users.Domain.Entities;

namespace EventHub.Users.Application.Abstractions.Identity;

public interface ITokenGenerator
{
    string GenerateToken(User user);
}
