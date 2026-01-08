using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Mars.Host.Features;

public class FeatureGateSwaggerFilter : IDocumentFilter
{
    private readonly IFeatureManager _featureManager;

    public FeatureGateSwaggerFilter(IFeatureManager featureManager)
    {
        _featureManager = featureManager;
    }

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var features = context.ApiDescriptions
            .Where(apiDesc => apiDesc.ActionDescriptor.EndpointMetadata
                .OfType<FeatureGateAttribute>()
                .Any())
            .ToList();

        foreach (var feature in features)
        {
            var featureGate = feature.ActionDescriptor.EndpointMetadata
                .OfType<FeatureGateAttribute>()
                .FirstOrDefault();

            if (featureGate != null && !_featureManager.IsEnabledAsync(featureGate.Features.First()).Result)
            {
                var route = "/" + feature.RelativePath.TrimEnd('/');
                swaggerDoc.Paths.Remove(route);
            }
        }
    }
}
