using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console(new CompactJsonFormatter()));

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHealthChecks();
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: "gateway"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options => options.Filter = IsTraceableHttpRequest)
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

WebApplication app = builder.Build();

app.MapReverseProxy();
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint();

app.UseSwaggerUI(options =>
{
    options.DocumentTitle = "EventHub API Gateway";
    options.SwaggerEndpoint("/docs/users/swagger/v1/swagger.json", "Users API v1");
    options.SwaggerEndpoint("/docs/events/swagger/v1/swagger.json", "Events API v1");
    options.SwaggerEndpoint("/docs/bookings/swagger/v1/swagger.json", "Bookings API v1");
});

await app.RunAsync();

static bool IsTraceableHttpRequest(HttpContext context) =>
    !context.Request.Path.StartsWithSegments("/metrics")
    && !context.Request.Path.StartsWithSegments("/health")
    && !context.Request.Path.StartsWithSegments("/swagger")
    && !context.Request.Path.StartsWithSegments("/docs");
