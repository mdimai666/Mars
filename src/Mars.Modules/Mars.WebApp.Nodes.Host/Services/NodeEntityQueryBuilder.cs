using Mars.Cms.Abstractions.Services;
using Mars.Nodes.Core.Models.EntityQuery;
using Mars.QueryLang;
using Mars.QueryLang.Contracts;

namespace Mars.WebApp.Nodes.Host.Services;

public class NodeEntityQueryBuilder
{
    private readonly IReadOnlyCollection<LinqMethodSignature> _signatures;
    private readonly IReadOnlyDictionary<string, NodeEntityModelProviderInfo> _providers;
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;
    private readonly IDatabaseEntityTypeCatalogService _databaseEntityTypeCatalogService;

    public NodeEntityQueryBuilder(IMetaModelTypesLocator metaModelTypesLocator,
                                    IQueryLangHelperAvailableMethodsProvider queryLangHelperAvailableMethods,
                                    IDatabaseEntityTypeCatalogService databaseEntityTypeCatalogService)
    {
        _metaModelTypesLocator = metaModelTypesLocator;
        _databaseEntityTypeCatalogService = databaseEntityTypeCatalogService;
        _signatures = queryLangHelperAvailableMethods.LinqMethodSignatures();
        _providers = ListProviders().ToDictionary(s => s.EntityName);
    }

    private IReadOnlyCollection<NodeEntityModelProviderInfo> ListProviders()
    {
        var list = new List<NodeEntityModelProviderInfo>();

        foreach (var (key, postType) in _metaModelTypesLocator.PostTypesDict())
        {
            list.Add(new() { EntityName = key, Group = "Post", Title = postType.Title, Methods = _signatures });
        }

        string[] publicTypes = ["Post", "User", "File", "NavMenu", "Option", "PostType", "Role", "UserType", "Feedback"];

        Func<string, string> groupName = (entity) => publicTypes.Contains(entity) ? entity : "internal";

        foreach (var prop in _databaseEntityTypeCatalogService.ListEntities())
        {
            list.Add(new()
            {
                EntityName = prop.EntityUri.Root!,
                Group = groupName(prop.EntityUri.Root!),
                Title = prop.EntityUri.Root!,
                Methods = _signatures
            });
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
