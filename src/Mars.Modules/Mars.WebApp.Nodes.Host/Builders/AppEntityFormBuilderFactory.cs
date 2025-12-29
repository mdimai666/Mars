using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Managers.Extensions;
using Mars.WebApp.Nodes.Front.Models.AppEntityForms;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebApp.Nodes.Host.Builders;

internal class AppEntityFormBuilderFactory : IAppEntityFormBuilderFactory, IDisposable
{
    internal const string FormsBuilderDictionaryCacheKey = "nodes:builders:AppEntityCreateFormsBuilderDictionary";

    private readonly IMemoryCache _memoryCache;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IEventManager _eventManager;
    private readonly IServiceScope _scope;

    IReadOnlyDictionary<string, Type> dict = new Dictionary<string, Type>()
    {
        ["Post"] = typeof(PostEntityCreateFormBuilder),
    };

    public AppEntityFormBuilderFactory(IMemoryCache memoryCache, IServiceScopeFactory serviceScopeFactory, IEventManager eventManager)
    {
        _memoryCache = memoryCache;
        _serviceScopeFactory = serviceScopeFactory;
        _eventManager = eventManager;
        _scope = _serviceScopeFactory.CreateScope();

        _eventManager.AddEventListener(_eventManager.Defaults.PostTypeAnyOperation(), (_) =>
        {
            _memoryCache.Remove(FormsBuilderDictionaryCacheKey);
        });
    }

    public IAppEntityCreateFormBuilder? GetBuilder(string name)
    {
        var type = dict.GetValueOrDefault(name);
        if (type == null) return null;
        return (IAppEntityCreateFormBuilder)ActivatorUtilities.CreateInstance(_scope.ServiceProvider, type);
    }

    public AppEntityCreateFormsBuilderDictionary FormsBuilderDictionary()
    {
        return _memoryCache.GetOrCreate(FormsBuilderDictionaryCacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            return CreateFormsBuilderDictionary(); ;
        })!;
    }

    public void Dispose()
    {
        _scope.Dispose();
    }

    private AppEntityCreateFormsBuilderDictionary CreateFormsBuilderDictionary()
    {
        var forms = dict.Keys.SelectMany(key => GetBuilder(key).AllForms()).ToList();

        return new() { Forms = forms };
    }
}

public interface IAppEntityFormBuilderFactory
{
    IAppEntityCreateFormBuilder? GetBuilder(string name);
    AppEntityCreateFormsBuilderDictionary FormsBuilderDictionary();

}
