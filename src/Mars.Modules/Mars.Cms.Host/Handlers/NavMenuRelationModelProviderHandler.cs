using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Dto.NavMenus;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;
using Mars.Contracts.Common;
using Mars.Contracts.Extensions;
using Mars.Contracts.Resources;
using Mars.Data.Extensions;

namespace Mars.Cms.Host.Handlers;

internal class NavMenuRelationModelProviderHandler(INavMenuRepository navMenuRepository) : IMetaRelationModelProviderHandler
{
    public async Task<Dictionary<Guid, object>> ListHandle(IReadOnlyCollection<Guid> ids, string modelName, CancellationToken cancellationToken)
    {
        return (await navMenuRepository.ListAll(new ListAllNavMenuQuery { Ids = ids }, cancellationToken)).ToDictionary(s => s.Id, s => (object)s);
    }

    public MetaRelationModel Structure()
    {
        return new MetaRelationModel
        {
            Key = "NavMenu",
            Title = "🧭 " + AppRes.NavMenu,
            TitlePlural = AppRes.NavMenus,
            SubTypes = []
        };
    }

    public async Task<ListDataResult<MetaValueRelationModelSummary>> ListData(MetaValueRelationModelsListQuery query, CancellationToken cancellationToken)
    {
        var data = await navMenuRepository.List(new() { Skip = query.Skip, Take = query.Take, Sort = query.Sort, Search = query.Search }, cancellationToken);
        return data.ToMap(ToModelSummary);
    }

    public async Task<IReadOnlyDictionary<Guid, MetaValueRelationModelSummary>> GetIds(string modelName, Guid[] ids, CancellationToken cancellationToken)
    {
        return (await navMenuRepository.ListAll(new() { Ids = ids }, cancellationToken))
                                    .ToDictionary(s => s.Id, ToModelSummary);
    }

    MetaValueRelationModelSummary ToModelSummary(NavMenuSummary value)
        => new()
        {
            Id = value.Id,
            Title = value.Title,
            Description = value.Slug,
            CreatedAt = value.CreatedAt,
        };

    public Task<int> DeleteMany(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        return navMenuRepository.DeleteMany(new() { Ids = ids }, cancellationToken);
    }
}
