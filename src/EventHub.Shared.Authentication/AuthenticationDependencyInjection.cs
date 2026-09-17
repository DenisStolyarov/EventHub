using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Shared.Authentication;

public static class AuthenticationDependencyInjection
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services
            .AddOptionsWithValidateOnStart<JwtValidationOptions>()
            .ValidateDataAnnotations()
            .BindConfiguration(JwtValidationOptions.SectionName);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.ConfigureOptions<JwtBearerConfiguration>();

        return services;
    }

    public static IServiceCollection AddCurrentUserService(this IServiceCollection services) =>
        services.AddScoped<ICurrentUserService, CurrentUserService>();
}
