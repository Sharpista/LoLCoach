using LoLCoach.Api.Application;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LoLCoach.Api.Infrastructure;

/// <summary>Projects SearchPlayerValidator's constraints into the OpenAPI schema.</summary>
public sealed class SearchPlayerSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(SearchPlayerCommand))
        {
            return;
        }

        schema.Required = new HashSet<string>(["gameName", "tagLine", "region"]);
        var gameName = schema.Properties["gameName"];
        gameName.MinLength = SearchPlayerValidator.GameNameMinLength;
        gameName.MaxLength = SearchPlayerValidator.GameNameMaxLength;
        gameName.Description = "Trimmed length 3-16; Unicode letters, digits, and spaces."
            + " Leading and trailing spaces are ignored by validation.";

        var tagLine = schema.Properties["tagLine"];
        tagLine.MinLength = SearchPlayerValidator.TagLineMinLength;
        tagLine.MaxLength = SearchPlayerValidator.TagLineMaxLength;
        tagLine.Pattern = SearchPlayerValidator.TagLineSchemaPattern;
        tagLine.Description = "Trimmed length 2-5; ASCII alphanumeric characters."
            + " Leading and trailing spaces are ignored by validation.";

        var region = schema.Properties["region"];
        region.Enum = RiotRegions.SupportedPlatforms
            .Select(platform => (IOpenApiAny)new OpenApiString(platform)).ToList();
        region.Description = "LoL platform code; comparison is case-insensitive and trimmed."
            + " The value is normalized to lowercase.";
    }
}