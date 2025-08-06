using System.Reflection;
using Mars.Host.Shared.Attributes;
using Mars.Host.Shared.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Services;

internal class PostContentProcessorsLocator : IPostContentProcessorsLocator
{
    private readonly IServiceScope _scope;
    private readonly IServiceCollection _serviceCollection;
    private readonly IMemoryCache _memoryCache;
    public const string CacheKey = "PostContentProcessorsLocator:List";

    public PostContentProcessorsLocator(IServiceScopeFactory serviceScopeFactory, IServiceCollection serviceCollection, IMemoryCache memoryCache)
    {
        _scope = serviceScopeFactory.CreateScope();
        _serviceCollection = serviceCollection;
        _memoryCache = memoryCache;
    }

    public IReadOnlyCollection<string> ListKeys(string[]? tags = null)
    {
        if (!_memoryCache.TryGetValue<KeyredHandlerAttribute[]>(CacheKey, out var cachedList))
        {
            var providers = _serviceCollection.Where(x => x.IsKeyedService
                                                && x.ServiceType == typeof(IPostContentProcessor)
                                                //&& typeof(IPostContentProcessor).IsAssignableFrom(x.ServiceType)
                                                )
                                    .Where(s => s.ServiceKey.GetType() == typeof(string))
                                    .ToArray();

            cachedList = providers.Select(s =>
            {
                var key = (string)s.ServiceKey!;
                var attr = s.KeyedImplementationType!.GetCustomAttribute<KeyredHandlerAttribute>()!;
                return new KeyredHandlerAttribute(attr.Key ?? key) { Tags = attr.Tags };
            }).ToArray();
        }

        if (tags?.Any() ?? false)
        {
            return cachedList!.Where(s => tags.All(t => s.Tags.Contains(t)))
                              .Select(s => s.Key)
                              .ToArray()!;
        }
        return cachedList!.Select(s => s.Key).ToArray()!;
    }

    public IPostContentProcessor? GetProvider(string postContentType)
    {
        return _scope.ServiceProvider.GetKeyedService<IPostContentProcessor>(postContentType);
    }

}
