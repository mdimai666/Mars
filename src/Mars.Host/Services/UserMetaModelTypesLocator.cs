using System.Collections.Frozen;
using System.Collections.Immutable;
using Mars.Host.Shared.Dto.UserTypes;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Services;

internal class UserMetaLocator : IUserMetaLocator//, IMarsAppLifetimeService
{
    private FrozenDictionary<string, UserTypeInfo>? _userTypes;
    private Dictionary<Guid, string>? _userTypesById;

    private readonly IServiceScope _scope;
    private readonly IUserTypeRepository _userTypeRepository;
    private SemaphoreSlim _lockUserTypes = new(1, 1);

    public UserMetaLocator(IServiceScopeFactory serviceScopeFactory)
    {
        _scope = serviceScopeFactory.CreateScope();
        _userTypeRepository = _scope.ServiceProvider.GetRequiredService<IUserTypeRepository>();
    }

    private async Task<FrozenDictionary<string, UserTypeInfo>> GetTypeData()
    {
        try
        {
            await _lockUserTypes.WaitAsync(1000);
            _userTypesById = [];

            var types = await _userTypeRepository.ListAllDetail(new(), CancellationToken.None);
            return types.ToFrozenDictionary(s => s.TypeName, s =>
            {
                _userTypesById.Add(s.Id, s.TypeName);
                return new UserTypeInfo
                {
                    UserType = s
                };
            });
        }
        finally
        {
            _lockUserTypes.Release();
        }
    }

    public UserTypeDetail? GetTypeDetailById(Guid id)
    {
        _userTypes ??= GetTypeData().ConfigureAwait(false).GetAwaiter().GetResult();

        var name = _userTypesById!.GetValueOrDefault(id);
        if (name == null) return null;

        return _userTypes.GetValueOrDefault(name)?.UserType;
    }

    public UserTypeDetail? GetTypeDetailByName(string userTypeName)
    {
        _userTypes ??= GetTypeData().ConfigureAwait(false).GetAwaiter().GetResult();
        return _userTypes.GetValueOrDefault(userTypeName)?.UserType;
    }

    public bool ExistType(Guid id)
    {
        _userTypes ??= GetTypeData().ConfigureAwait(false).GetAwaiter().GetResult();
        return _userTypesById.ContainsKey(id);
    }

    public bool ExistType(string userTypeName)
    {
        _userTypes ??= GetTypeData().ConfigureAwait(false).GetAwaiter().GetResult();
        return _userTypes.ContainsKey(userTypeName);
    }

    public IReadOnlyDictionary<string, UserTypeDetail> GetTypeDict()
    {
        _userTypes ??= GetTypeData().ConfigureAwait(false).GetAwaiter().GetResult();
        return _userTypes.ToDictionary(s => s.Key, s => s.Value.UserType);
    }

    private async Task InitializeCache()
    {
        _userTypes ??= await GetTypeData();
    }

    public void InvalidateCompiledMetaMtoModels()
    {
        _userTypes = null;

    }

    //[StartupOrder(10)]
    //public Task OnStartupAsync()
    //{
    //    _ = InitializeCache();
    //    return Task.CompletedTask;
    //}
}

public record UserTypeInfo
{
    public required UserTypeDetail UserType { get; init; }

}
