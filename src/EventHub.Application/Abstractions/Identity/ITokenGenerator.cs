using EventHub.Domain.Entities;

namespace EventHub.Application.Abstractions.Identity;

public interface ITokenGenerator
{
    string GenerateToken(User user);
}
