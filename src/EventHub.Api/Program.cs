using EventHub.Api.Application;
using EventHub.Api.Infrastructure;
using EventHub.Api.Presentation;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddPresentation();

if (builder.Environment.IsDevelopment())
{
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });
}

WebApplication app = builder.Build();

app.UsePresentation();

await app.RunAsync();