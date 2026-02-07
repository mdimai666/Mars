using System.Collections.Frozen;
using System.Collections.Immutable;
using Mars.Host.Shared.Dto.PostCategoryTypes;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Services;

internal class PostCategoryMetaLocator : IPostCategoryMetaLocator, IMarsAppLifetimeService
{
    private FrozenDictionary<string, PostCategoryTypeInfo>? _postCategoryTypes;
    private Dictionary<Guid, string>? _postCategoryTypesById;

    private readonly IServiceScope _scope;
    private readonly IPostCategoryTypeRepository _postCategoryTypeRepository;
    private SemaphoreSlim _lockPostTypes = new(1, 1);

    public PostCategoryMetaLocator(IServiceScopeFactory serviceScopeFactory)
    {
        _scope = serviceScopeFactory.CreateScope();
        _postCategoryTypeRepository = _scope.ServiceProvider.GetRequiredService<IPostCategoryTypeRepository>();
    }

    private async Task<FrozenDictionary<string, PostCategoryTypeInfo>> GetTypeData()
    {
        try
        {
            await _lockPostTypes.WaitAsync(1000);
            _postCategoryTypesById = [];

            var types = await _postCategoryTypeRepository.ListAllDetail(new(), CancellationToken.None);
            return types.ToFrozenDictionary(s => s.TypeName, s =>
            {
                _postCategoryTypesById.Add(s.Id, s.TypeName);
                return new PostCategoryTypeInfo
                {
                    PostCategoryType = s
                };
            });
        }
        finally
        {
            _lockPostTypes.Release();
        }
    }

    public PostCategoryTypeDetail? GetTypeDetailById(Guid id)
    {
        _postCategoryTypes ??= GetTypeData().ConfigureAwait(false).GetAwaiter().GetResult();

        var name = _postCategoryTypesById!.GetValueOrDefault(id);
        if (name == null) return null;

        return _postCategoryTypes.GetValueOrDefault(name)?.PostCategoryType;
    }

    public PostCategoryTypeDetail? GetTypeDetailByName(string postCategoryTypeName)
    {
        _postCategoryTypes ??= GetTypeData().ConfigureAwait(false).GetAwaiter().GetResult();
        return _postCategoryTypes.GetValueOrDefault(postCategoryTypeName)?.PostCategoryType;
    }

    public bool ExistType(Guid id)
    {
        _postCategoryTypes ??= GetTypeData().ConfigureAwait(false).GetAwaiter().GetResult();
        return _postCategoryTypesById.ContainsKey(id);
    }

    public bool ExistType(string postCategoryTypeName)
    {
        _postCategoryTypes ??= GetTypeData().ConfigureAwait(false).GetAwaiter().GetResult();
        return _postCategoryTypes.ContainsKey(postCategoryTypeName);
    }

    public IReadOnlyDictionary<string, PostCategoryTypeDetail> GetTypeDict()
    {
        _postCategoryTypes ??= GetTypeData().ConfigureAwait(false).GetAwaiter().GetResult();
        return _postCategoryTypes.ToDictionary(s => s.Key, s => s.Value.PostCategoryType);
    }

    private async Task InitializeCache()
    {
        _postCategoryTypes ??= await GetTypeData();
    }

    public void InvalidateCompiledMetaMtoModels()
    {
        _postCategoryTypes = null;

    }

    [StartupOrder(10)]
    public Task OnStartupAsync()
    {
        _ = InitializeCache();
        return Task.CompletedTask;
    }
}

public record PostCategoryTypeInfo
{
    public required PostCategoryTypeDetail PostCategoryType { get; init; }

}
