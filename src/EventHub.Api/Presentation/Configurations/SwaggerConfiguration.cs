using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EventHub.Api.Presentation.Configurations;

public class SwaggerConfiguration(IApiVersionDescriptionProvider provider) : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (ApiVersionDescription description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new()
            {
                Title = $"EventHub API {description.GroupName}",
                Version = description.ApiVersion.ToString()
            });
        }
    }
}