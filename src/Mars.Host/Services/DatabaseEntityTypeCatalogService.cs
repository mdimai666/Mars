using System.Reflection;
using Mars.Core.Extensions;
using Mars.Host.Data.Contexts;
using Mars.Host.Shared.Services;
using Mars.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Services;

public class DatabaseEntityTypeCatalogService : IDatabaseEntityTypeCatalogService
{
    IReadOnlyDictionary<string, PropertyInfo>? _memberDbSetsByName;
    IReadOnlyDictionary<string, PropertyInfo>? _memberDbSetsByTypeName;
    IReadOnlyDictionary<Type, PropertyInfo>? _memberDbSetsByType;
    IReadOnlyDictionary<SourceUri, EntitySourceResult>? _marsDbContextEntitiesDict;

    protected IReadOnlyDictionary<string, PropertyInfo> DbSetsByPropertyName
        => _memberDbSetsByName ??= typeof(MarsDbContext).GetProperties()
                .Where(p => p.PropertyType.IsGenericType
                            && (p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)))
                .ToDictionary(s => s.Name);

    protected IReadOnlyDictionary<Type, PropertyInfo> DbSetsByEntityType
        => _memberDbSetsByType ??= DbSetsByPropertyName.Values.ToDictionary(s => s.PropertyType.GenericTypeArguments[0]);

    protected IReadOnlyDictionary<string, PropertyInfo> DbSetsByEntityTypeName
        => _memberDbSetsByTypeName ??= DbSetsByEntityType.ToDictionary(s => s.Key.Name, s => s.Value);

    protected IReadOnlyDictionary<SourceUri, EntitySourceResult> DbSetsByEntityUri
        => _marsDbContextEntitiesDict ??= DbSetsByEntityType.Select(s => new EntitySourceResult
        {
            EntityUri = s.Key.Name.TrimSubstringEnd("Entity"),
            IsMetaType = false,
            MetaEntityModelType = s.Key,
        }).ToDictionary(s => s.EntityUri);

    public EntitySourceResult? ResolveName(string entityName)
    {
        var xEntityType = FindEntityDbSetByPropertyName(entityName);
        xEntityType ??= FindEntityDbSetByTypeName(entityName);
        xEntityType ??= FindEntityDbSetByTypeName(entityName + "Entity");

        if (xEntityType is null) return null;

        return new()
        {
            //EntityUri = new SourceUri($"ef://{efPropertyName}"),
            MetaEntityModelType = xEntityType,
            IsMetaType = false,
            EntityUri = $"/{xEntityType.Name[..^6]}",// remove "Entity" suffix
        };

    }

    #region Tools
    Type? FindEntityDbSetByPropertyName(string entityName)
    {
        return DbSetsByPropertyName.GetValueOrDefault(entityName)?.PropertyType.GenericTypeArguments[0];
    }

    Type? FindEntityDbSetByTypeName(string typeName)
    {
        return DbSetsByEntityTypeName.GetValueOrDefault(typeName)?.PropertyType.GenericTypeArguments[0];
    }
    #endregion

    public IReadOnlyCollection<EntitySourceResult> ListEntities()
    {
        _marsDbContextEntitiesDict ??= DbSetsByEntityType.Select(s => new EntitySourceResult
        {
            EntityUri = "/" + s.Key.Name.TrimSubstringEnd("Entity"),
            IsMetaType = false,
            MetaEntityModelType = s.Key,
        }).ToDictionary(s => s.EntityUri);

        return _marsDbContextEntitiesDict.Values.ToList();
    }

    public EntitySourceResult? Entity(SourceUri sourceUri)
        => DbSetsByEntityUri.GetValueOrDefault(sourceUri);

}
