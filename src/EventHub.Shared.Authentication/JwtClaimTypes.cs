using Microsoft.IdentityModel.JsonWebTokens;

namespace EventHub.Shared.Authentication;

public static class JwtClaimTypes
{
    public const string Name = JwtRegisteredClaimNames.Name;

    public const string Sub = JwtRegisteredClaimNames.Sub;

    public const string Role = "role";
}
