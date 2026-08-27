using Mars.Cms.Abstractions;
using Mars.Core.Exceptions;
using Mars.Core.Extensions;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Dto.PostCategories;
using Mars.Cms.Abstractions.Dto.PostCategoryTypes;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Host.Services;
using Mars.Identity.Abstractions.Interfaces;
using Mars.Server.Abstractions.Validators;
using Mars.Server.Abstractions.Managers;
using Mars.Server.Abstractions.Managers.Extensions;
using Mars.Cms.Abstractions.Mappings.PostCategories;
using Mars.Cms.Abstractions.Mappings.PostCategoryTypes;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Host.Services;
using Mars.Identity.Abstractions.Interfaces;
using Mars.Server.Abstractions.Validators;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Host.Services;
using Mars.Identity.Abstractions.Interfaces;
using Mars.Server.Abstractions.Validators;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Host.Services;
using Mars.Identity.Abstractions.Interfaces;
using Mars.Server.Abstractions.Validators;
using Mars.Contracts.Common;
using Mars.Contracts.MetaFields;
using Mars.Contracts.PostCategories;

namespace Mars.Cms.Host.Services;

internal class PostCategoryService : IPostCategoryService
{
    private readonly IPostCategoryRepository _postRepository;
    private readonly IPostCategoryMetaLocator _postCategoryMetaLocator;
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;
    private readonly IEventManager _eventManager;
    private readonly IRequestContext _requestContext;
    private readonly IValidatorFactory _validatorFactory;

    public PostCategoryService(
        IPostCategoryRepository postRepository,
        IPostCategoryMetaLocator postCategoryMetaLocator,
        IMetaModelTypesLocator metaModelTypesLocator,
        IEventManager eventManager,
        IRequestContext requestContext,
        IValidatorFactory validatorFactory)
    {
        _postRepository = postRepository;
        _postCategoryMetaLocator = postCategoryMetaLocator;
        _metaModelTypesLocator = metaModelTypesLocator;
        _eventManager = eventManager;
        _requestContext = requestContext;
        _validatorFactory = validatorFactory;
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
        await _validatorFactory.ValidateAndThrowAsync(query, cancellationToken);
        return await _postRepository.List(query, cancellationToken);
    }

    public async Task<PagingResult<PostCategorySummary>> ListTable(ListPostCategoryQuery query, CancellationToken cancellationToken)
    {
        await _validatorFactory.ValidateAndThrowAsync(query, cancellationToken);
        return await _postRepository.ListTable(query, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PostCategorySummary>> ListSummaryByIds(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.None()) return [];
        return await _postRepository.ListAll(new() { Type = null, PostTypeName = null, Ids = ids }, cancellationToken);
    }

    public async Task<PostCategoryDetail> Create(CreatePostCategoryQuery query, CancellationToken cancellationToken)
    {
        await _validatorFactory.ValidateAndThrowAsync(query, cancellationToken);

        var id = await _postRepository.Create(query, cancellationToken);
        var created = (await GetDetail(id, cancellationToken))!;

        var payload = new ManagerEventPayload(_eventManager.Defaults.PostCategoryAdd(), created);//TODO: сделать явный тип.
        _eventManager.TriggerEvent(payload);

        return created;
    }

    public async Task<PostCategoryDetail> Update(UpdatePostCategoryQuery query, CancellationToken cancellationToken)
    {
        await _validatorFactory.ValidateAndThrowAsync(query, cancellationToken);

        await _postRepository.Update(query, cancellationToken);
        var updated = (await GetDetail(query.Id, cancellationToken))!;

        var payload = new ManagerEventPayload(_eventManager.Defaults.PostCategoryUpdate(), updated);
        _eventManager.TriggerEvent(payload);

        return updated;
    }

    public async Task<PostCategorySummary> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _validatorFactory.ValidateAndThrowAsync<Guid, DeletePostCategoryQueryValidator>(id, cancellationToken);

        var post = await Get(id, cancellationToken) ?? throw new NotFoundException();

        await _postRepository.Delete(id, cancellationToken);

        var payload = new ManagerEventPayload(_eventManager.Defaults.PostCategoryDelete(), post);
        _eventManager.TriggerEvent(payload);
        return post;
    }

    public async Task<IReadOnlyCollection<PostCategorySummary>> DeleteMany(DeleteManyPostCategoryQuery query, CancellationToken cancellationToken)
    {
        await _validatorFactory.ValidateAndThrowAsync(query, cancellationToken);

        var posts = await _postRepository.ListAll(new() { Type = null, PostTypeName = null, Ids = query.Ids }, cancellationToken);

        await _postRepository.DeleteMany(query, cancellationToken);

        foreach (var post in posts)
        {
            var payload = new ManagerEventPayload(_eventManager.Defaults.PostCategoryDelete(), post);
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
            category = category with { MetaValues = MetaValuesEnricher.EnrichWithBlankMetaValuesFromMetaValues(category.MetaValues, postCategoryType.MetaFields) };
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
            category = category with { MetaValues = MetaValuesEnricher.EnrichWithBlankMetaValuesFromMetaValues(category.MetaValues, categoryTypeDetail.MetaFields) };
        }

        return Task.FromResult<PostCategoryEditViewModel>(new()
        {
            PostCategory = category.ToResponse(),
            PostCategoryType = categoryTypeDetail.ToResponse()
        });
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
