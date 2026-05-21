using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using EventHub.Api.Presentation.Configurations;
using EventHub.Api.Presentation.ExceptionHandlers;

namespace EventHub.Api.Presentation;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddProblemDetails();
        services.AddSwaggerGen();
        services.AddEndpointsApiExplorer();
        
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.ConfigureOptions<SwaggerConfiguration>();

        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Api-Version"),
                new QueryStringApiVersionReader("api-version"));
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
        });

        return services;
    }

    public static WebApplication UsePresentation(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();

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
                        $"EventHub API {description.GroupName.ToUpperInvariant()}");
                }
            });
        }

        app.MapControllers();

        return app;
    }
}