using Mars.Nodes.Core.Models.EntityQuery;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Host.Services;

public class NodeEditorToolServce
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _memoryCache;

    public NodeEditorToolServce(IServiceProvider serviceProvider, IMemoryCache memoryCache)
    {
        _serviceProvider = serviceProvider;
        _memoryCache = memoryCache;
    }

    public NodeEntityQueryBuilderDictionary NodeEntityQueryBuilderDictionary()
    {
        var dict = _memoryCache.GetOrCreate("nodes:providers:NodeEntityQueryBuilder", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            var builder = ActivatorUtilities.CreateInstance<NodeEntityQueryBuilder>(_serviceProvider);
            //var builder = NodeEntityQueryBuilder()
            return builder.CreateDictionary();
        })!;

        return dict;
    }

}
