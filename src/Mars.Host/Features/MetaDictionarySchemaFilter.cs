using System.Text.Json;
using System.Text.Json.Nodes;
using Mars.Core.Features.JsonConverter;
using Mars.Shared.Contracts.PostJsons;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Mars.Host.Features;

public class MetaDictionarySchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(IReadOnlyDictionary<string, object?>))
        {
            schema.Type = "object";
            schema.AdditionalProperties = new OpenApiSchema
            {
                OneOf =
                [
                    new OpenApiSchema { Type = "string" },
                    new OpenApiSchema { Type = "number" },
                    new OpenApiSchema { Type = "integer" },
                    new OpenApiSchema { Type = "boolean" },
                    new OpenApiSchema { Type = "object" },
                    new OpenApiSchema { Type = "array", Items = new OpenApiSchema { Type = "string" } }
                ]
            };
            schema.Example = new OpenApiObject
            {
                ["key1"] = new OpenApiInteger(123),
                ["key2"] = new OpenApiString("value"),
                ["key3"] = new OpenApiBoolean(true),
                //["key4"] = new OpenApiArray
                //{
                //    new OpenApiString("item1"),
                //    new OpenApiString("item2")
                //}
            };
        }
    }
}

public class PostJsonExamplesFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody?.Content != null)
        {
            var requestType = context.ApiDescription.ParameterDescriptions
                .FirstOrDefault(p => p.Source == BindingSource.Body)?.Type;

            if (requestType == typeof(CreatePostJsonRequest))
            {
                var example = new CreatePostJsonRequest
                {
                    Id = null,
                    Title = "Sample Post",
                    Type = "post",
                    Slug = null,
                    Tags = ["test"],
                    Content = "",
                    Status = null,
                    Excerpt = null,
                    LangCode = null,
                    Meta = new Dictionary<string, JsonValue>
                    {
                        ["int"] = JsonValue.Create(123),
                        ["bool"] = JsonValue.Create(true),
                        ["str"] = JsonValue.Create("string")
                    }
                };

                var json = JsonSerializer.Serialize(example, SystemJsonConverter.DefaultJsonSerializerOptions());

                operation.RequestBody.Content["application/json"].Example = OpenApiAnyFactory
                    .CreateFromJson(json);
            }
            else if (requestType == typeof(UpdatePostJsonRequest))
            {
                var example = new UpdatePostJsonRequest
                {
                    Id = Guid.Empty,
                    Title = "Sample Post",
                    Type = "post",
                    Slug = "slug",
                    Tags = ["test"],
                    Content = "",
                    Status = null,
                    Excerpt = null,
                    LangCode = null,
                    Meta = new Dictionary<string, JsonValue>
                    {
                        ["int"] = JsonValue.Create(123),
                        ["bool"] = JsonValue.Create(true),
                        ["str"] = JsonValue.Create("string")
                    }
                };

                var json = JsonSerializer.Serialize(example, SystemJsonConverter.DefaultJsonSerializerOptions());

                operation.RequestBody.Content["application/json"].Example = OpenApiAnyFactory
                    .CreateFromJson(json);
            }
        }
    }
}
