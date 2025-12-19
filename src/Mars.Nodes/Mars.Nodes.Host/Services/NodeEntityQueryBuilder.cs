using System.Reflection;
using Mars.Host.Data.Contexts;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core.Models.EntityQuery;
using Mars.QueryLang;
using Microsoft.EntityFrameworkCore;

namespace Mars.Nodes.Host.Services;

public class NodeEntityQueryBuilder
{
    private readonly IReadOnlyCollection<LinqMethodSignarute> _signarutes;
    private readonly IReadOnlyDictionary<string, NodeEntityModelProviderInfo> _providers;
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;
    private Dictionary<string, PropertyInfo>? _memberDbSetsByName;

    public NodeEntityQueryBuilder(IMetaModelTypesLocator metaModelTypesLocator, IQueryLangHelperAvailableMethodsProvider queryLangHelperAvailableMethods)
    {
        _metaModelTypesLocator = metaModelTypesLocator;
        _signarutes = queryLangHelperAvailableMethods.LinqMethodSignarutes();
        _providers = ListProviders().ToDictionary(s => s.EntityName);
    }

    private IReadOnlyCollection<NodeEntityModelProviderInfo> ListProviders()
    {
        var list = new List<NodeEntityModelProviderInfo>();

        foreach (var (key, postType) in _metaModelTypesLocator.PostTypesDict())
        {
            list.Add(new() { EntityName = key, Group = nameof(MarsDbContext.Posts), Title = postType.Title, Methods = _signarutes });
        }

        _memberDbSetsByName ??= typeof(MarsDbContext).GetProperties()
                .Where(p => p.PropertyType.IsGenericType
                            && (p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)))
                .ToDictionary(s => s.Name);

        string[] publicTypes = ["Posts", "Users", "Files", "NavMenus", "Options", "PostTypes", "Roles", "UserTypes", "Feedbacks"];

        Func<string, string> groupName = (entity) => publicTypes.Contains(entity) ? entity : "internal";

        foreach (var prop in _memberDbSetsByName)
        {
            list.Add(new() { EntityName = prop.Key, Group = groupName(prop.Key), Title = prop.Key, Methods = _signarutes });
        }

        return list;
    }

    public NodeEntityQueryBuilderDictionary CreateDictionary()
    {
        return new()
        {
            Providers = _providers
        };
    }
}
