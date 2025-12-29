using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Managers.Extensions;
using Mars.Nodes.Core.Models.EntityQuery;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Host.Services;

public class NodeEditorToolServce
{
    private const string NodeEntityQueryBuilderCacheKey = "nodes:providers:NodeEntityQueryBuilder";

    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _memoryCache;
    private readonly IEventManager _eventManager;

    public NodeEditorToolServce(IServiceProvider serviceProvider, IMemoryCache memoryCache, IEventManager eventManager)
    {
        _serviceProvider = serviceProvider;
        _memoryCache = memoryCache;
        _eventManager = eventManager;

        _eventManager.AddEventListener(_eventManager.Defaults.PostTypeAnyOperation(), (_) =>
        {
            _memoryCache.Remove(NodeEntityQueryBuilderCacheKey);
        });
    }

    public NodeEntityQueryBuilderDictionary NodeEntityQueryBuilderDictionary()
    {
        var dict = _memoryCache.GetOrCreate(NodeEntityQueryBuilderCacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            var builder = ActivatorUtilities.CreateInstance<NodeEntityQueryBuilder>(_serviceProvider);
            //var builder = NodeEntityQueryBuilder()
            return builder.CreateDictionary();
        })!;

        return dict;
    }

}
