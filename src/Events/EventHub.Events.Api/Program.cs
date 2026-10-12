using EventHub.Events.Api.Presentation;
using EventHub.Events.Application;
using EventHub.Events.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.SetupLogging();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddPresentation();
builder.Services.AddObservability("events-service");

if (builder.Environment.IsDevelopment())
{
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });
}

WebApplication app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.UsePresentation();

await app.RunAsync();
