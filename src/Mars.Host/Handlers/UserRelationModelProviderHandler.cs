using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Mars.Shared.Resources;

namespace Mars.Host.Handlers;

internal class UserRelationModelProviderHandler(IUserRepository userRepository) : IMetaRelationModelProviderHandler
{
    public async Task<Dictionary<Guid, object>> ListHandle(IReadOnlyCollection<Guid> ids, string modelName, CancellationToken cancellationToken)
    {
        return (await userRepository.ListAllDetail(new ListAllUserQuery { Ids = ids }, cancellationToken)).ToDictionary(s => s.Id, s => (object)s);
    }

    public MetaRelationModel Structure()
    {
        return new MetaRelationModel
        {
            Key = "User",
            Title = "👤 " + AppRes.User,
            TitlePlural = AppRes.Users,
            SubTypes = []
        };
    }

    public async Task<ListDataResult<MetaValueRelationModelSummary>> ListData(MetaValueRelationModelsListQuery query, CancellationToken cancellationToken)
    {
        var data = await userRepository.ListDetail(new() { Skip = query.Skip, Take = query.Take, Sort = query.Sort, Search = query.Search }, cancellationToken);
        return data.ToMap(ToModelSummary);
    }

    public async Task<IReadOnlyDictionary<Guid, MetaValueRelationModelSummary>> GetIds(string modelName, Guid[] ids, CancellationToken cancellationToken)
    {
        return (await userRepository.ListAllDetail(new() { Ids = ids }, cancellationToken))
                                    .ToDictionary(s => s.Id, ToModelSummary);
    }

    MetaValueRelationModelSummary ToModelSummary(UserDetail value)
        => new()
        {
            Id = value.Id,
            Title = value.FullName,
            Description = value.UserName,
            CreatedAt = value.CreatedAt,
        };

    public Task<int> DeleteMany(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        return userRepository.DeleteMany(new() { Ids = ids }, cancellationToken);
    }
}
