namespace EventHub.Shared.Authentication;

public sealed class JwtValidationOptions : JwtOptions
{
    public bool ValidateIssuer { get; set; } = true;

    public bool ValidateAudience { get; set; } = true;

    public bool ValidateLifetime { get; set; } = true;

    public bool ValidateIssuerSigningKey { get; set; } = true;

    public TimeSpan ClockSkew { get; set; } = TimeSpan.Zero;
}
