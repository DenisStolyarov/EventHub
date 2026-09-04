using System.Security.Claims;
using System.Text;
using EventHub.Application.Abstractions.Identity;
using EventHub.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace EventHub.Infrastructure.Identity;

public sealed class JwtTokenGenerator(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenGenerator
{
    private JwtOptions Options { get; } = options.Value;

    public string GenerateToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        List<Claim> claims = [
            new(JwtClaimTypes.Name, user.Login),
            new(JwtClaimTypes.Sub, user.Id.ToString()),
            new(JwtClaimTypes.Role, user.Role.ToString())
        ];

        DateTime expires = timeProvider.GetUtcNow().UtcDateTime.AddMinutes(Options.ExpiryMinutes);

        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(Options.Secret));
        SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

        SecurityTokenDescriptor tokenDescriptor = new()
        {
            Subject = new(claims),
            Issuer = Options.Issuer,
            Audience = Options.Audience,
            Expires = expires,
            SigningCredentials = credentials
        };

        return new JsonWebTokenHandler().CreateToken(tokenDescriptor);
    }
}
