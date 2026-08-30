using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;
using Mars.Contracts.Common;
using Mars.Contracts.Extensions;
using Mars.Contracts.Resources;

namespace Mars.Cms.Host.Handlers;

internal class PostRelationModelProviderHandler(IPostRepository postRepository,
                                                IMetaModelTypesLocator modelTypesLocator,
                                                IPostService postService,
                                                IPostMetaColumnsService postMetaColumnsService)
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

        var imageUrls = await GetImageUrlsAsync(data.Items, cancellationToken);
        return data.ToMap(s => ToModelSummary(s, imageUrls));
    }

    public async Task<IReadOnlyDictionary<Guid, MetaValueRelationModelSummary>> GetIds(string modelName, Guid[] ids, CancellationToken cancellationToken)
    {
        var subtypeModelName = modelName == "Post" ? null : modelName.Split('.', 2)[1];

        var posts = await postRepository.ListAll(new() { Ids = ids, Type = subtypeModelName }, cancellationToken);
        var imageUrls = await GetImageUrlsAsync(posts, cancellationToken);

        return posts.ToDictionary(s => s.Id, s => ToModelSummary(s, imageUrls));
    }

    MetaValueRelationModelSummary ToModelSummary(PostSummary value, IReadOnlyDictionary<Guid, string> imageUrls)
        => new()
        {
            Id = value.Id,
            Title = value.Title,
            Description = value.Slug,
            CreatedAt = value.CreatedAt,
            ImageUrl = imageUrls.GetValueOrDefault(value.Id),
        };

    async Task<Dictionary<Guid, string>> GetImageUrlsAsync(IEnumerable<PostSummary> posts, CancellationToken cancellationToken)
    {
        var urls = new Dictionary<Guid, string>();

        foreach (var group in posts.GroupBy(s => s.Type))
        {
            var imageKey = modelTypesLocator.GetPostTypeByName(group.Key)?.ImageFieldKey;
            if (string.IsNullOrEmpty(imageKey)) continue;

            var ids = group.Select(s => s.Id).ToArray();
            var values = await postMetaColumnsService.GetDisplayValuesAsync(group.Key, [imageKey], ids, cancellationToken);
            foreach (var (postId, row) in values)
            {
                var url = row.GetValueOrDefault(imageKey);
                if (!string.IsNullOrEmpty(url)) urls[postId] = url;
            }
        }

        return urls;
    }

    //TODO: переместить сюда PostTypes list sub types

    public async Task<int> DeleteMany(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        var posts = await postService.DeleteMany(new() { Ids = ids }, cancellationToken);

        return posts.Count;
    }
}
