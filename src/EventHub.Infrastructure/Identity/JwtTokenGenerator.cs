using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EventHub.Application.Abstractions.Identity;
using EventHub.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EventHub.Infrastructure.Identity;

public sealed class JwtTokenGenerator(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenGenerator
{
    private JwtOptions Options { get; } = options.Value;

    public string GenerateToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        DateTime expires = timeProvider.GetUtcNow().UtcDateTime.AddMinutes(Options.ExpiryMinutes);

        List<Claim> claims = [
            new (ClaimTypes.Name, user.Login),
            new (ClaimTypes.NameIdentifier, user.Id.ToString()),
            new (ClaimTypes.Role, user.Role.ToString())
        ];

        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(Options.Secret));
        SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new(
            issuer: Options.Issuer,
            audience: Options.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        string jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return jwt;
    }
}
