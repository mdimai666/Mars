using System.Reflection;
using Microsoft.FeatureManagement.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Mars.Host.Features;

public class FeatureGateOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var featureGateAttributes = context.MethodInfo.GetCustomAttributes<FeatureGateAttribute>(true);
        if (!featureGateAttributes.Any())
        {
            featureGateAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes<FeatureGateAttribute>(true);
        }

        if (featureGateAttributes?.Any() ?? false)
        {
            var features = string.Join(", ", featureGateAttributes.SelectMany(a => a.Features));
            operation.Description += $"\n\n**Feature flags required:** {features}";
        }
    }
}
