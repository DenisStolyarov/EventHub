using System.Text.Json.Serialization;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using EventHub.Bookings.Api.Presentation.Configurations;
using EventHub.Bookings.Api.Presentation.ExceptionHandlers;
using EventHub.Shared.Authentication;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;

namespace EventHub.Bookings.Api.Presentation;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddControllers()
                .AddEnumJsonConverter();
        services.AddProblemDetails();
        services.AddHttpContextAccessor();
        services.AddJwtAuthentication();
        services.AddVersioning();
        services.AddSwagger();
        services.AddHealthChecks();

        services.AddCurrentUserService();

        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }

    public static WebApplication UsePresentation(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseSwaggerMiddleware();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health");
        app.MapPrometheusScrapingEndpoint();

        return app;
    }

    public static void SetupLogging(this WebApplicationBuilder builder) =>
        builder.Host.UseSerilog((context, configuration) =>
            configuration.ReadFrom.Configuration(context.Configuration)
                .WriteTo.Console(new CompactJsonFormatter()));

    public static IServiceCollection AddObservability(this IServiceCollection services, string serviceName)
    {
        services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(serviceName: serviceName))
                .WithTracing(tracing => tracing
                    .AddAspNetCoreInstrumentation(options => options.Filter = IsTraceableHttpRequest)
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation(options => options.Filter = IsTraceableDbCommand)
                    .AddOtlpExporter())
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddPrometheusExporter());

        return services;
    }

    private static bool IsTraceableHttpRequest(HttpContext context) =>
        !context.Request.Path.StartsWithSegments("/metrics")
        && !context.Request.Path.StartsWithSegments("/health")
        && !context.Request.Path.StartsWithSegments("/swagger");

    private static bool IsTraceableDbCommand(string? providerName, System.Data.IDbCommand command) =>
        !(command.CommandText.Contains("outbox_messages", StringComparison.OrdinalIgnoreCase)
        && command.CommandText.Contains("processed_at IS NULL", StringComparison.OrdinalIgnoreCase));

    private static IMvcBuilder AddEnumJsonConverter(this IMvcBuilder builder)
    {
        builder.AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        return builder;
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
                        $"EventHub Bookings API {description.GroupName.ToUpperInvariant()}");
                }
            });
        }
    }
}
