using System.ComponentModel.DataAnnotations;

namespace EventHub.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";

    public bool ValidateIssuer { get; set; } = true;

    public bool ValidateAudience { get; set; } = true;

    public bool ValidateLifetime { get; set; } = true;

    public bool ValidateIssuerSigningKey { get; set; } = true;

    [Required]
    [Url]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int ExpiryMinutes { get; set; } = 15;

    [Required]
    [MinLength(32)]
    public string Secret { get; set; } = string.Empty;

    public TimeSpan ClockSkew { get; set; } = TimeSpan.Zero;
}
