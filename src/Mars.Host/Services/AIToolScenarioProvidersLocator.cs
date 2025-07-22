using System.Reflection;
using Mars.Host.Shared.Attributes;
using Mars.Host.Shared.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Services;

internal class AIToolScenarioProvidersLocator : IAIToolScenarioProvidersLocator
{
    private readonly IServiceScope _scope;
    private readonly IServiceCollection _serviceCollection;
    private readonly IMemoryCache _memoryCache;
    public const string ScenariosListCacheKey = "AIToolScenarioProvidersLocator:ScenariosList";

    public AIToolScenarioProvidersLocator(IServiceScopeFactory serviceScopeFactory, IServiceCollection serviceCollection, IMemoryCache memoryCache)
    {
        _scope = serviceScopeFactory.CreateScope();
        _serviceCollection = serviceCollection;
        _memoryCache = memoryCache;
    }

    public IReadOnlyCollection<string> ListProviderKeys(string[]? tags = null)
    {
        if (!_memoryCache.TryGetValue<RegisterAIToolAttribute[]>(ScenariosListCacheKey, out var cachedList))
        {
            var providers = _serviceCollection.Where(x => x.IsKeyedService
                                                && x.ServiceType == typeof(IAIToolScenarioProvider)
                                                //&& typeof(IAIToolScenarioProvider).IsAssignableFrom(x.ServiceType)
                                                )
                                    .Where(s => s.ServiceKey.GetType() == typeof(string))
                                    .ToArray();

            cachedList = providers.Select(s =>
            {
                var key = (string)s.ServiceKey!;
                var attr = s.KeyedImplementationType!.GetCustomAttribute<RegisterAIToolAttribute>()!;
                return new RegisterAIToolAttribute { Key = attr.Key ?? key, Tags = attr.Tags };
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

    public IAIToolScenarioProvider? GetProvider(string key)
    {
        return _scope.ServiceProvider.GetKeyedService<IAIToolScenarioProvider>(key);
    }

}
