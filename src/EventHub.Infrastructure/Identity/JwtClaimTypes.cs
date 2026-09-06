using Microsoft.IdentityModel.JsonWebTokens;

namespace EventHub.Infrastructure.Identity;

public static class JwtClaimTypes
{
    public const string Name = JwtRegisteredClaimNames.Name;

    public const string Sub = JwtRegisteredClaimNames.Sub;

    public const string Role = "role";
}
