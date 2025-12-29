using Mars.Shared.Models;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Mars.Host.Features;

public class SourceUriSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(SourceUri))
        {
            schema.Type = "string";
            schema.Format = null;

            schema.Example = new OpenApiString("/Post/post");

            schema.Properties?.Clear();
            schema.AdditionalPropertiesAllowed = false;
        }
    }
}
