using System.Text.Json;
using System.Text.Json.Nodes;
using Mars.Core.Features.JsonConverter;
using Mars.Shared.Contracts.PostJsons;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Mars.Host.Features;

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

                operation.RequestBody.Content["application/json"].Example = json;
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

                operation.RequestBody.Content["application/json"].Example = json;
            }
        }
    }
}
