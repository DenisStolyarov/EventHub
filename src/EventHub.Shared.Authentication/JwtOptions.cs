using System.ComponentModel.DataAnnotations;

namespace EventHub.Shared.Authentication;

public class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";

    [Required]
    [Url]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Required]
    [MinLength(32)]
    public string Secret { get; set; } = string.Empty;
}
