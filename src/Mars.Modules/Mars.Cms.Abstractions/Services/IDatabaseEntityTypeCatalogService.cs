using Mars.Shared.Models;

namespace Mars.Host.Shared.Services;

public interface IDatabaseEntityTypeCatalogService
{
    IReadOnlyCollection<EntitySourceResult> ListEntities();
    EntitySourceResult? Entity(SourceUri sourceUri);
    EntitySourceResult? ResolveName(string entityName);
}

public record EntitySourceResult
{
    public required SourceUri EntityUri { get; init; }
    public required Type MetaEntityModelType { get; init; }
    public required bool IsMetaType { get; init; }
}
