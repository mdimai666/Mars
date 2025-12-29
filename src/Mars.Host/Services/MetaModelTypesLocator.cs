using System.Collections.Frozen;
using System.Collections.Immutable;
using FluentValidation;
using Mars.Core.Extensions;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Services;

internal class MetaModelTypesLocator : IMetaModelTypesLocator, IMarsAppLifetimeService
{
    private FrozenDictionary<string, PostTypeInfo>? _postTypes;
    private Dictionary<Guid, string>? _postTypesById;

    private readonly IServiceScope _scope;
    private readonly IPostTypeRepository _postTypeRepository;
    private readonly IServiceCollection _serviceCollection;
    private readonly IMetaEntityTypeProvider _metaEntityTypeProvider;
    private readonly IDatabaseEntityTypeCatalogService _databaseEntityTypeCatalogService;
    public SemaphoreSlim _lockPostTypes = new(1, 1);

    public MetaModelTypesLocator(IServiceScopeFactory serviceScopeFactory,
                                IServiceCollection serviceCollection,
                                IMetaEntityTypeProvider metaEntityTypeProvider,
                                IDatabaseEntityTypeCatalogService databaseEntityTypeCatalogService)
    {
        _scope = serviceScopeFactory.CreateScope();
        _postTypeRepository = _scope.ServiceProvider.GetRequiredService<IPostTypeRepository>();
        _serviceCollection = serviceCollection;
        _metaEntityTypeProvider = metaEntityTypeProvider;
        _databaseEntityTypeCatalogService = databaseEntityTypeCatalogService;
    }

    private async Task<FrozenDictionary<string, PostTypeInfo>> GetPostTypes()
    {
        try
        {
            await _lockPostTypes.WaitAsync(1000);
            _postTypesById = [];

            var types = await _postTypeRepository.ListAllDetail(CancellationToken.None);
            return types.ToFrozenDictionary(s => s.TypeName, s =>
            {
                _postTypesById.Add(s.Id, s.TypeName);
                return new PostTypeInfo
                {
                    PostType = s
                };
            });
        }
        finally
        {
            _lockPostTypes.Release();
        }
    }

    public PostTypeDetail? GetPostTypeById(Guid id)
    {
        _postTypes ??= GetPostTypes().ConfigureAwait(false).GetAwaiter().GetResult();

        var name = _postTypesById!.GetValueOrDefault(id);
        if (name == null) return null;

        return _postTypes.GetValueOrDefault(name)?.PostType;
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

    IReadOnlyDictionary<string, Type>? _metaRelationModelProviderDict;

    public IReadOnlyCollection<string> ListMetaRelationModelProviderKeys()
    {
        _metaRelationModelProviderDict ??= _serviceCollection.Where(x => x.IsKeyedService
                                            && x.ServiceType == typeof(IMetaRelationModelProviderHandler)
                                            //&& typeof(IMetaRelationModelProviderHandler).IsAssignableFrom(x.ServiceType)
                                            )
                                .Where(s => s.ServiceKey.GetType() == typeof(string))
                                .ToDictionary(g => (string)g.ServiceKey!, g => g.ServiceType);

        return _metaRelationModelProviderDict.Keys is IReadOnlyCollection<string> collection
                    ? collection
                    : _metaRelationModelProviderDict.Keys.ToArray();
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

    IReadOnlyDictionary<string, MtoModelInfo>? _metaMtoModelsCompiledTypeDict;
    string? _metaMtoModelsCompiledSourceCode = null;

    public IReadOnlyDictionary<string, MtoModelInfo> MetaMtoModelsCompiledTypeDict => _metaMtoModelsCompiledTypeDict ?? ImmutableDictionary<string, MtoModelInfo>.Empty;

    private object _metaMtoModelsCompiledTypeDictLock = new();

    public void UpdateMetaModelMtoRuntimeCompiledTypes()
    {
        var result = _metaEntityTypeProvider.GenerateMetaTypes().ConfigureAwait(false).GetAwaiter().GetResult();

        _metaMtoModelsCompiledTypeDict = result.ToDictionary(s => s.KeyName);
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

    public MetaModelSourceResult? ResolveEntityNameToSourceUri(string entityName)
    {
        var databaseEntity = _databaseEntityTypeCatalogService.ResolveName(entityName);
        if (databaseEntity is not null) return new()
        {
            EntityUri = databaseEntity.EntityUri,
            BaseEntityType = databaseEntity.MetaEntityModelType,
            IsMetaType = databaseEntity.IsMetaType,
            MetaEntityModelType = databaseEntity.MetaEntityModelType,
        };

        TryUpdateMetaModelMtoRuntimeCompiledTypes();

        var metaMtoType = MetaMtoModelsCompiledTypeDict.GetValueOrDefault(entityName);
        if (metaMtoType is not null)
        {
            return new MetaModelSourceResult
            {
                EntityUri = $"/{metaMtoType.BaseEntityType.Name.TrimSubstringEnd("Entity")}/{metaMtoType.KeyName}",
                MetaEntityModelType = metaMtoType.CreatedType,
                IsMetaType = true,
                BaseEntityType = metaMtoType.BaseEntityType,
            };
        }

        return null;
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
