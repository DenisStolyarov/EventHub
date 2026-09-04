using System.Text;
using EventHub.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EventHub.Api.Presentation.Configurations;

public sealed class JwtBearerConfiguration(IOptions<JwtOptions> jwtOptions) : IConfigureOptions<JwtBearerOptions>
{
    private JwtOptions Options { get; } = jwtOptions.Value;

    public void Configure(JwtBearerOptions options)
    {
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(Options.Secret));

        options.TokenValidationParameters = new()
        {
            ValidateIssuer = Options.ValidateIssuer,
            ValidIssuer = Options.Issuer,

            ValidateAudience = Options.ValidateAudience,
            ValidAudience = Options.Audience,

            ValidateIssuerSigningKey = Options.ValidateIssuerSigningKey,
            IssuerSigningKey = key,

            ValidateLifetime = Options.ValidateLifetime,
            ClockSkew = Options.ClockSkew
        };
    }
}
