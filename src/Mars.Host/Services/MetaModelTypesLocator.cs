using System.Collections.Frozen;
using FluentValidation;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.QueryLang;

internal class MetaModelTypesLocator : IMetaModelTypesLocator, IMarsAppLifetimeService
{
    private FrozenDictionary<string, PostTypeInfo>? _postTypes;

    private readonly IServiceScope _scope;
    private readonly IPostTypeRepository _postTypeRepository;
    private readonly IServiceCollection _serviceCollection;
    private readonly IMetaEntityTypeProvider _metaEntityTypeProvider;
    public SemaphoreSlim _lockPostTypes = new(1, 1);

    public MetaModelTypesLocator(IServiceScopeFactory serviceScopeFactory, IServiceCollection serviceCollection, IMetaEntityTypeProvider metaEntityTypeProvider)
    {
        _scope = serviceScopeFactory.CreateScope();
        _postTypeRepository = _scope.ServiceProvider.GetRequiredService<IPostTypeRepository>();
        _serviceCollection = serviceCollection;
        _metaEntityTypeProvider = metaEntityTypeProvider;
    }

    private async Task<FrozenDictionary<string, PostTypeInfo>> GetPostTypes()
    {
        try
        {
            await _lockPostTypes.WaitAsync(1000);

            var types = await _postTypeRepository.ListAllDetail(CancellationToken.None);
            return types.ToFrozenDictionary(s => s.TypeName, s => new PostTypeInfo
            {
                PostType = s
            });
        }
        finally
        {
            _lockPostTypes.Release();
        }
    }

    public PostTypeDetail? GetPostTypeByName(string postTypeName)
    {
        _postTypes ??= GetPostTypes().ConfigureAwait(false).GetAwaiter().GetResult();
        return _postTypes.GetValueOrDefault(postTypeName)?.PostType;
    }

    public IReadOnlyDictionary<string, PostTypeDetail> PostTypesDict()
    {
        _postTypes ??= GetPostTypes().ConfigureAwait(false).GetAwaiter().GetResult();
        return _postTypes.ToDictionary(s => s.Key, s => s.Value.PostType);
    }

    private async Task InitializeCache()
    {
        _postTypes ??= await GetPostTypes();
    }

    public IReadOnlyCollection<string> ListMetaRelationModelProviderKeys()
    {
        var providers = _serviceCollection.Where(x => x.IsKeyedService
                                            && x.ServiceType == typeof(IMetaRelationModelProviderHandler)
                                            //&& typeof(IMetaRelationModelProviderHandler).IsAssignableFrom(x.ServiceType)
                                            )
                                .Where(s => s.ServiceKey.GetType() == typeof(string))
                                .ToDictionary(g => g.ServiceKey!);

        return providers.Select(s => (string)s.Key).ToList();
    }

    public IMetaRelationModelProviderHandler? GetMetaRelationModelProvider(string modelName)
    {
        return _scope.ServiceProvider.GetKeyedService<IMetaRelationModelProviderHandler>(modelName);
    }

    public IReadOnlyCollection<MetaRelationModel> AllMetaRelationsStructure() //TODO: add cache
    {
        var keys = ListMetaRelationModelProviderKeys();

        var list = new List<MetaRelationModel>(keys.Count);

        foreach (var key in keys)
        {
            var provider = GetMetaRelationModelProvider(key);
            list.Add(provider.Structure());
        }

        return list;
    }

    public void InvalidateCompiledMetaMtoModels()
    {
        _postTypes = null;
        _metaMtoModelsCompiledTypeDict = null;
        _metaMtoModelsCompiledSourceCode = null;

        //TODO: для PostType добавить VersionToken. B повесить хук, который инвалидирует при изменении
    }

    Dictionary<string, Type>? _metaMtoModelsCompiledTypeDict = null;
    string? _metaMtoModelsCompiledSourceCode = null;

    public Dictionary<string, Type> MetaMtoModelsCompiledTypeDict => _metaMtoModelsCompiledTypeDict ?? new();

    private object _metaMtoModelsCompiledTypeDictLock = new();

    public void UpdateMetaModelMtoRuntimeCompiledTypes()
    {
        Dictionary<string, Type> result = _metaEntityTypeProvider.GenerateMetaTypes().ConfigureAwait(false).GetAwaiter().GetResult();

        _metaMtoModelsCompiledTypeDict = result;
    }

    public async Task<string> MetaTypesSourceCode(string lang = "csharp")
    {
        if (lang != "csharp") throw new NotImplementedException("support only 'csharp' is now");

        if (_metaMtoModelsCompiledSourceCode == null)
        {
            _metaMtoModelsCompiledSourceCode = await _metaEntityTypeProvider.GenerateMetaTypesSourceCode();
        }

        return _metaMtoModelsCompiledSourceCode;
    }

    /// <summary>
    /// Update only if doct is NULL
    /// </summary>
    public void TryUpdateMetaModelMtoRuntimeCompiledTypes()
    {
        if (_metaMtoModelsCompiledTypeDict is not null) return;

        lock (_metaMtoModelsCompiledTypeDictLock)
        {
            if (_metaMtoModelsCompiledTypeDict is null)
            {
                UpdateMetaModelMtoRuntimeCompiledTypes();
            }
        }
    }

    [StartupOrder(10)]
    public Task OnStartupAsync()
    {
        _ = InitializeCache();
        return Task.CompletedTask;
    }
}

public record PostTypeInfo
{
    public required PostTypeDetail PostType { get; init; }

}
