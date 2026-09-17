using EventHub.Bookings.Api.Presentation;
using EventHub.Bookings.Application;
using EventHub.Bookings.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
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

await app.Services.InitializeDatabaseAsync();

app.UsePresentation();

await app.RunAsync();
