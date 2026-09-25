using Mars.Nodes.Front.Abstractions.Services;

namespace Mars.Nodes.Workspace.Services.ValueFields;

internal class ValueFieldProvider(IEnumerable<IValueRootProvider> providers) : IValueFieldProvider
{
    public IReadOnlyCollection<ValueFieldInfo> GetFields(ValueFieldContext context)
        => [.. providers
            .OrderBy(provider => provider.Order)
            .SelectMany(provider => provider.GetFields(context))
            .DistinctBy(field => field.Path)];
}
