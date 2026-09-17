using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EventHub.Shared.Authentication;

public sealed class JwtBearerConfiguration(IOptions<JwtValidationOptions> jwtOptions) : IConfigureNamedOptions<JwtBearerOptions>
{
    private JwtValidationOptions Options { get; } = jwtOptions.Value;

    public void Configure(string? name, JwtBearerOptions options) => Configure(options);

    public void Configure(JwtBearerOptions options)
    {
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(Options.Secret));

        options.MapInboundClaims = false;
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = Options.ValidateIssuer,
            ValidIssuer = Options.Issuer,

            ValidateAudience = Options.ValidateAudience,
            ValidAudience = Options.Audience,

            ValidateIssuerSigningKey = Options.ValidateIssuerSigningKey,
            IssuerSigningKey = key,

            ValidateLifetime = Options.ValidateLifetime,
            ClockSkew = Options.ClockSkew,

            NameClaimType = JwtClaimTypes.Name,
            RoleClaimType = JwtClaimTypes.Role,
        };
    }
}
