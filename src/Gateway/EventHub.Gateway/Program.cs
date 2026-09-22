WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHealthChecks();

WebApplication app = builder.Build();

app.MapReverseProxy();
app.MapHealthChecks("/health");

app.UseSwaggerUI(options =>
{
    options.DocumentTitle = "EventHub API Gateway";
    options.SwaggerEndpoint("/docs/users/swagger/v1/swagger.json", "Users API v1");
    options.SwaggerEndpoint("/docs/events/swagger/v1/swagger.json", "Events API v1");
    options.SwaggerEndpoint("/docs/bookings/swagger/v1/swagger.json", "Bookings API v1");
});

await app.RunAsync();
