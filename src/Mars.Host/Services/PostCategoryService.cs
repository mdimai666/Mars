using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.PostCategories;
using Mars.Host.Shared.Dto.PostCategoryTypes;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Managers.Extensions;
using Mars.Host.Shared.Mappings.PostCategories;
using Mars.Host.Shared.Mappings.PostCategoryTypes;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Validators;
using Mars.Shared.Common;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.PostCategories;

namespace Mars.Host.Services;

internal class PostCategoryService : IPostCategoryService
{
    private readonly IPostCategoryRepository _postRepository;
    private readonly IPostCategoryMetaLocator _postCategoryMetaLocator;
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;
    private readonly IEventManager _eventManager;
    private readonly IRequestContext _requestContext;
    private readonly IValidatorFabric _validatorFabric;

    public PostCategoryService(
        IPostCategoryRepository postRepository,
        IPostCategoryMetaLocator postCategoryMetaLocator,
        IMetaModelTypesLocator metaModelTypesLocator,
        IEventManager eventManager,
        IRequestContext requestContext,
        IValidatorFabric validatorFabric)
    {
        _postRepository = postRepository;
        _postCategoryMetaLocator = postCategoryMetaLocator;
        _metaModelTypesLocator = metaModelTypesLocator;
        _eventManager = eventManager;
        _requestContext = requestContext;
        _validatorFabric = validatorFabric;
    }

    public Task<PostCategorySummary?> Get(Guid id, CancellationToken cancellationToken)
        => _postRepository.Get(id, cancellationToken);

    public Task<PostCategoryDetail?> GetDetail(Guid id, CancellationToken cancellationToken = default)
    {
        var post = _postRepository.GetDetail(id, cancellationToken);

        return post;
    }

    public Task<PostCategoryDetail?> GetDetailBySlug(string slug, string type, CancellationToken cancellationToken)
    {
        var post = _postRepository.GetDetailBySlug(slug, type, cancellationToken);

        return post;
    }

    public async Task<ListDataResult<PostCategorySummary>> List(ListPostCategoryQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);
        return await _postRepository.List(query, cancellationToken);
    }

    public async Task<PagingResult<PostCategorySummary>> ListTable(ListPostCategoryQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);
        return await _postRepository.ListTable(query, cancellationToken);
    }

    public async Task<PostCategoryDetail> Create(CreatePostCategoryQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);

        var id = await _postRepository.Create(query, cancellationToken);
        var created = await GetDetail(id, cancellationToken);

        var payload = new ManagerEventPayload(_eventManager.Defaults.PostCategoryAdd(created.Type), created);//TODO: сделать явный тип.
        _eventManager.TriggerEvent(payload);

        return created;
    }

    public async Task<PostCategoryDetail> Update(UpdatePostCategoryQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);

        await _postRepository.Update(query, cancellationToken);
        var updated = await GetDetail(query.Id, cancellationToken);

        var payload = new ManagerEventPayload(_eventManager.Defaults.PostCategoryUpdate(updated.Type), updated);
        _eventManager.TriggerEvent(payload);

        return updated;
    }

    public async Task<PostCategorySummary> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync<Guid, DeletePostCategoryQueryValidator>(id, cancellationToken);

        var post = await Get(id, cancellationToken) ?? throw new NotFoundException();

        await _postRepository.Delete(id, cancellationToken);

        var payload = new ManagerEventPayload(_eventManager.Defaults.PostCategoryDelete(post.Type), post);
        _eventManager.TriggerEvent(payload);
        return post;
    }

    public async Task<IReadOnlyCollection<PostCategorySummary>> DeleteMany(DeleteManyPostCategoryQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);

        var posts = await _postRepository.ListAll(new() { Type = null, PostTypeName = null, Ids = query.Ids }, cancellationToken);

        await _postRepository.DeleteMany(query, cancellationToken);

        foreach (var post in posts)
        {
            var payload = new ManagerEventPayload(_eventManager.Defaults.PostCategoryDelete(post.Type), post);
            _eventManager.TriggerEvent(payload);
        }
        return posts;
    }

    #region EDIT_MODEL
    public async Task<PostCategoryEditViewModel> GetEditModel(Guid id, CancellationToken cancellationToken)
    {
        var category = await _postRepository.GetPostCategoryEditDetail(id, cancellationToken) ?? throw new NotFoundException();
        var postCategoryType = _postCategoryMetaLocator.GetTypeDetailByName(category.Type);

        if (category.MetaValues.Count != postCategoryType.MetaFields.Count)
        {
            category = category with { MetaValues = EnrichWithBlankMetaValuesFromMetaValues(category.MetaValues, postCategoryType.MetaFields) };
        }

        return new()
        {
            PostCategory = category.ToResponse(),
            PostCategoryType = postCategoryType.ToResponse()
        };
    }

    public Task<PostCategoryEditViewModel> GetEditModelBlank(string categoryType, string postType, CancellationToken cancellationToken)
    {
        var postTypeDetail = _metaModelTypesLocator.GetPostTypeByName(postType) ?? throw new NotFoundException();
        var categoryTypeDetail = _postCategoryMetaLocator.GetTypeDetailByName(categoryType) ?? throw new NotFoundException();

        var category = GetPostCategoryBlank(categoryType, postType);

        if (category.MetaValues.Count != categoryTypeDetail.MetaFields.Count)
        {
            category = category with { MetaValues = EnrichWithBlankMetaValuesFromMetaValues(category.MetaValues, categoryTypeDetail.MetaFields) };
        }

        return Task.FromResult<PostCategoryEditViewModel>(new()
        {
            PostCategory = category.ToResponse(),
            PostCategoryType = categoryTypeDetail.ToResponse()
        });
    }

    /// <summary>
    /// Обогощает незаполненные MataFields
    /// </summary>
    /// <param name="metaValues"></param>
    /// <param name="metaFields"></param>
    /// <returns></returns>
    public static IReadOnlyCollection<MetaValueDetailDto> EnrichWithBlankMetaValuesFromMetaValues(
                                                            IEnumerable<MetaValueDetailDto> metaValues,
                                                            IReadOnlyCollection<MetaFieldDto> metaFields)
    {

        var valuesDictByMfId = metaValues.DistinctBy(s => s.MetaField.Id)
                                        .ToDictionary(s => s.MetaField.Id);

        var enrichMetaValues = new List<MetaValueDetailDto>(metaFields.Count);

        foreach (var mf in metaFields)
        {
            if (valuesDictByMfId.TryGetValue(mf.Id, out var mv))
            {
                enrichMetaValues.Add(mv);
            }
            else
            {
                //meta value not set. Create blank
                var blankMetaValue = GetBlankMetaValue(mf);
                enrichMetaValues.Add(blankMetaValue);
            }
        }

        return enrichMetaValues;
    }

    public static MetaValueDetailDto GetBlankMetaValue(MetaFieldDto metaField)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            Index = 0,
            ParentId = Guid.Empty,

            Bool = false,
            Int = 0,
            Float = 0,
            Decimal = 0,
            Long = 0,
            DateTime = DateTime.MinValue,
            ModelId = Guid.Empty,
            NULL = false,
            StringShort = metaField.Type == MetaFieldType.String ? "" : null,
            StringText = metaField.Type == MetaFieldType.Text ? "" : null,
            MetaField = metaField,
            VariantId = Guid.Empty,
            VariantsIds = []
        };
    }

    public PostCategoryEditDetail GetPostCategoryBlank(string categoryType, string postType)
    {
        var user = _requestContext.User;

        return new()
        {
            Id = Guid.Empty,
            Slug = "",
            Title = "",
            CreatedAt = DateTimeOffset.Now,
            ModifiedAt = null,
            Tags = [],
            Type = categoryType,
            PostType = postType,

            ParentId = null,
            PathIds = [],
            Disabled = false,

            MetaValues = []
        };
    }
    #endregion

    public CreatePostCategoryQuery EnrichQuery(CreatePostCategoryRequest request)
    {
        new PostCategoryTypeNameValidator(request.Type, _postCategoryMetaLocator);
        var postCategoryType = _postCategoryMetaLocator.GetTypeDetailByName(request.Type);
        var postType = _metaModelTypesLocator.GetPostTypeByName(request.PostType);

        var createQuery = request.ToQuery(postCategoryType.Id, postType.Id, postCategoryType.MetaFields.ToDictionary(s => s.Id));

        return createQuery;
    }

    public UpdatePostCategoryQuery EnrichQuery(UpdatePostCategoryRequest request)
    {
        new PostCategoryTypeNameValidator(request.Type, _postCategoryMetaLocator);
        var postCategoryType = _postCategoryMetaLocator.GetTypeDetailByName(request.Type);
        var postType = _metaModelTypesLocator.GetPostTypeByName(request.PostType);

        var updateQuery = request.ToQuery(postCategoryType.Id, postType.Id, postCategoryType.MetaFields.ToDictionary(s => s.Id));

        return updateQuery;
    }

}
