using System.ComponentModel.DataAnnotations;
using EventHub.Shared.Authentication;

namespace EventHub.Users.Infrastructure.Identity;

public sealed class JwtIssuerOptions : JwtOptions
{
    [Range(1, 1440)]
    public int ExpiryMinutes { get; set; } = 15;
}
