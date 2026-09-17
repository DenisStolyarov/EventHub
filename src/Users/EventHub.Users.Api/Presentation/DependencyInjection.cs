using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using EventHub.Users.Api.Presentation.Configurations;
using EventHub.Users.Api.Presentation.ExceptionHandlers;

namespace EventHub.Users.Api.Presentation;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddProblemDetails();
        services.AddEndpointsApiExplorer();
        services.AddVersioning();
        services.AddSwagger();

        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }

    public static WebApplication UsePresentation(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseSwaggerMiddleware();

        app.MapControllers();

        return app;
    }

    private static void AddVersioning(this IServiceCollection services) =>
        services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Api-Version"),
                new QueryStringApiVersionReader("api-version"));
        })
        .AddMvc()
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

    private static void AddSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen();

        services.ConfigureOptions<SwaggerConfiguration>();
    }

    private static void UseSwaggerMiddleware(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            IApiVersionDescriptionProvider provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                foreach (ApiVersionDescription description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint(
                        $"/swagger/{description.GroupName}/swagger.json",
                        $"EventHub Users API {description.GroupName.ToUpperInvariant()}");
                }
            });
        }
    }
}
