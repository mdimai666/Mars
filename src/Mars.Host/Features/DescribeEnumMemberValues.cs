using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Mars.Host.Features;

public class DescribeEnumMemberValues : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            schema.Enum.Clear();

            //Retrieve each of the values decorated with an EnumMember attribute
            foreach (var v in context.Type.GetEnumValues())
            {
                //var memberAttr = member.GetCustomAttributes(typeof(EnumMemberAttribute), false).FirstOrDefault();
                //if (memberAttr != null)
                //{
                //    var attr = (EnumMemberAttribute)memberAttr;
                //    schema.Enum.Add(new OpenApiString(attr.Value));
                //}
                //if (member.GetType().IsEnum)
                {
                    object val = Convert.ChangeType(v, Type.GetTypeCode(context.Type));
                    schema.Enum.Add(JsonValue.Create($"{val} = {v}"));
                }
            }
        }
    }
}
