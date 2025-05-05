using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Mars.Shared.Resources;

namespace Mars.Host.Handlers;

internal class PostRelationModelProviderHandler(IPostRepository postRepository, IMetaModelTypesLocator modelTypesLocator)
    : IMetaRelationModelProviderHandler, IMetaRelationModelProviderWithSubItemsHandler
{
    public async Task<Dictionary<Guid, object>> ListHandle(IReadOnlyCollection<Guid> ids, string modelName, CancellationToken cancellationToken)
    {
        var subtypeModelName = modelName == "Post" ? null : modelName.Split('.', 2)[1];

        return (await postRepository.ListAllDetail(new() { Ids = ids, Type = subtypeModelName }, cancellationToken))
                                    .ToDictionary(s => s.Id, s => (object)s);
    }

    public MetaRelationModel Structure()
    {
        return new MetaRelationModel
        {
            Key = "Post",
            Title = "✏️ " + AppRes.Post,
            TitlePlural = AppRes.Posts,
            SubTypes = ListSubTypes().ConfigureAwait(false).GetAwaiter().GetResult()
        };
    }

    public Task<RelationModelSubType[]> ListSubTypes()
    {
        var subTypes = modelTypesLocator.PostTypesDict();

        return Task.FromResult(subTypes.Values.Select(s => new RelationModelSubType
        {
            Key = $"Post.{s.TypeName}",
            Title = s.Title,
            TitlePlural = $"{s.Title}'ы"
        }).ToArray());
    }

    public async Task<ListDataResult<MetaValueRelationModelSummary>> ListData(MetaValueRelationModelsListQuery query, CancellationToken cancellationToken)
    {
        var subtypeModelName = query.ModelName == "Post" ? null : query.ModelName.Split('.', 2)[1];

        var data = await postRepository.List(new()
        {
            Skip = query.Skip,
            Take = query.Take,
            Sort = query.Sort,
            Search = query.Search,

            Type = subtypeModelName,

        }, cancellationToken);
        return data.ToMap(ToModelSummary);
    }

    public async Task<IReadOnlyDictionary<Guid, MetaValueRelationModelSummary>> GetIds(string modelName, Guid[] ids, CancellationToken cancellationToken)
    {
        var subtypeModelName = modelName == "Post" ? null : modelName.Split('.', 2)[1];

        return (await postRepository.ListAll(new() { Ids = ids, Type = subtypeModelName }, cancellationToken))
                                    .ToDictionary(s => s.Id, ToModelSummary);
    }

    MetaValueRelationModelSummary ToModelSummary(PostSummary value)
        => new()
        {
            Id = value.Id,
            Title = value.Title,
            Description = value.Slug,
            CreatedAt = value.CreatedAt,
        };

    //TODO: переместить сюда PostTypes list sub types
}
