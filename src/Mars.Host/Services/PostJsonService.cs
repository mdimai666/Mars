using System.Text.Json;
using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Mappings.Posts;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Validators;
using Mars.Shared.Common;

namespace Mars.Host.Services;

internal class PostJsonService : IPostJsonService
{
    private readonly IPostRepository _postRepository;
    private readonly IValidatorFabric _validatorFabric;
    private readonly IMetaFieldMaterializerService _metaFieldMaterializer;
    static JsonSerializerOptions _serializerOptions = new();

    public PostJsonService(
        IPostRepository postRepository,
        IValidatorFabric validatorFabric,
        IMetaFieldMaterializerService metaFieldMaterializer)
    {
        _postRepository = postRepository;
        _validatorFabric = validatorFabric;
        _metaFieldMaterializer = metaFieldMaterializer;
    }

    public async Task<PostJsonDto?> GetDetail(Guid id, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetDetail(id, cancellationToken);
        if (post == null) return null;

        var fillDict = await _metaFieldMaterializer.GetFillContext(post.MetaValues, cancellationToken);

        return post?.ToJsonDto(fillDict);
    }

    public async Task<PostJsonDto?> GetDetailBySlug(string slug, string type, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetDetailBySlug(slug, type, cancellationToken);
        if (post == null) return null;

        var fillDict = await _metaFieldMaterializer.GetFillContext(post.MetaValues, cancellationToken);

        return post?.ToJsonDto(fillDict);
    }

    public async Task<ListDataResult<PostJsonDto>> List(ListPostQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);
        var list = await _postRepository.ListDetail(query, cancellationToken);
        var fillDict = await _metaFieldMaterializer.GetFillContext(list.Items.SelectMany(s => s.MetaValues), cancellationToken);
        return list.ToMap(s => s.ToJsonDtoList(fillDict));
    }

    public async Task<PagingResult<PostJsonDto>> ListTable(ListPostQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);
        var list = await _postRepository.ListTableDetail(query, cancellationToken);
        var fillDict = await _metaFieldMaterializer.GetFillContext(list.Items.SelectMany(s => s.MetaValues), cancellationToken);
        return list.ToMap(s => s.ToJsonDtoList(fillDict));
    }

}
