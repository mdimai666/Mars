using Mars.Core.Exceptions;
using Mars.Core.Extensions;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Managers.Extensions;
using Mars.Host.Shared.Mappings.Posts;
using Mars.Host.Shared.Mappings.PostTypes;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Validators;
using Mars.Shared.Common;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.Posts;
using Mars.Shared.Contracts.PostTypes;

namespace Mars.Host.Services;

internal class PostService : IPostService
{
    private readonly IPostRepository _postRepository;
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;
    private readonly IEventManager _eventManager;
    private readonly IRequestContext _requestContext;
    private readonly IValidatorFabric _validatorFabric;

    public PostService(
        IPostRepository postRepository,
        IMetaModelTypesLocator metaModelTypesLocator,
        IEventManager eventManager,
        IRequestContext requestContext,
        IValidatorFabric validatorFabric)
    {
        _postRepository = postRepository;
        _metaModelTypesLocator = metaModelTypesLocator;
        _eventManager = eventManager;
        _requestContext = requestContext;
        _validatorFabric = validatorFabric;
    }

    public Task<PostSummary?> Get(Guid id, CancellationToken cancellationToken)
        => _postRepository.Get(id, cancellationToken);

    public Task<PostDetail?> GetDetail(Guid id, CancellationToken cancellationToken)
        => _postRepository.GetDetail(id, cancellationToken);

    public Task<PostDetail?> GetDetailBySlug(string slug, string type, CancellationToken cancellationToken)
        => _postRepository.GetDetailBySlug(slug, type, cancellationToken);

    public async Task<ListDataResult<PostSummary>> List(ListPostQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);
        return await _postRepository.List(query, cancellationToken);
    }

    public async Task<PagingResult<PostSummary>> ListTable(ListPostQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);
        return await _postRepository.ListTable(query, cancellationToken);
    }

    public async Task<PostDetail> Create(CreatePostQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);

        var id = await _postRepository.Create(query, cancellationToken);
        var created = await GetDetail(id, cancellationToken);

        //if (created != null)
        {
            var payload = new ManagerEventPayload(_eventManager.Defaults.PostAdd(created.Type), created);//TODO: сделать явный тип.
            _eventManager.TriggerEvent(payload);
        }

        return created;
    }

    public async Task<PostDetail> Update(UpdatePostQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);
        //await _validatorFabric.ValidateAndThrowAsync<UpdatePostQueryValidator, UpdatePostQuery>(query, cancellationToken);

        await _postRepository.Update(query, cancellationToken);
        var updated = await GetDetail(query.Id, cancellationToken);

        var payload = new ManagerEventPayload(_eventManager.Defaults.PostUpdate(updated.Type), updated);
        _eventManager.TriggerEvent(payload);

        return updated;
    }

    public async Task<UserActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var post = await Get(id, cancellationToken) ?? throw new NotFoundException();

        try
        {
            await _postRepository.Delete(id, cancellationToken);

            //if (result.Ok)
            {
                var payload = new ManagerEventPayload(_eventManager.Defaults.PostDelete(post.Type), post);
                _eventManager.TriggerEvent(payload);
            }
            return UserActionResult.Success();
        }
        catch (Exception ex)
        {
            return UserActionResult.Exception(ex);
        }
    }

    #region EDIT_MODEL
    public async Task<PostEditViewModel> GetEditModel(Guid id, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetPostEditDetail(id, cancellationToken) ?? throw new NotFoundException();
        var postType = _metaModelTypesLocator.GetPostTypeByName(post.Type);

        if (post.MetaValues.Count != postType.MetaFields.Count)
        {
            post = post with { MetaValues = EnrichWithBlankMetaValuesFromMetaValues(post.MetaValues, postType.MetaFields) };
        }

        return new()
        {
            Post = post.ToResponse(),
            PostType = postType.ToResponse()
        };
    }

    public Task<PostEditViewModel> GetEditModelBlank(string type, CancellationToken cancellationToken)
    {
        var postType = _metaModelTypesLocator.GetPostTypeByName(type) ?? throw new NotFoundException();

        var post = GetPostBlank(postType);

        if (post.MetaValues.Count != postType.MetaFields.Count)
        {
            post = post with { MetaValues = EnrichWithBlankMetaValuesFromMetaValues(post.MetaValues, postType.MetaFields) };
        }

        return Task.FromResult<PostEditViewModel>(new()
        {
            Post = post.ToResponse(),
            PostType = postType.ToResponse()
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

    public PostEditDetail GetPostBlank(PostTypeDetail postType)
    {
        var user = _requestContext.User;

        var isStatusSupport = postType.EnabledFeatures.Contains(PostTypeConstants.Features.Status);
        var status = (isStatusSupport ? postType.PostStatusList.FirstOrDefault()?.Slug : null) ?? "";

        var author = new PostAuthor()
        {
            Id = user?.Id ?? Guid.Empty,
            UserName = user?.UserName ?? "",
            DisplayName = string.Join(' ', Tools.TrimNulls([user?.LastName, user?.FirstName])),
        };

        return new()
        {
            Id = Guid.Empty,
            Slug = "",
            Title = "",
            Content = "",
            Excerpt = "",
            CreatedAt = DateTimeOffset.Now,
            ModifiedAt = null,
            LangCode = "",
            Status = status,
            Tags = [],
            Type = postType.TypeName,

            Author = author,
            MetaValues = []
        };
    }
    #endregion

    public CreatePostQuery EnrichQuery(CreatePostRequest request)
    {
        new PostTypeNameValidator(request.Type, _metaModelTypesLocator);
        var postType = _metaModelTypesLocator.GetPostTypeByName(request.Type);

        var createQuery = request.ToQuery(_requestContext.User.Id, postType.MetaFields.ToDictionary(s => s.Id));

        return createQuery;
    }

    public UpdatePostQuery EnrichQuery(UpdatePostRequest request)
    {
        new PostTypeNameValidator(request.Type, _metaModelTypesLocator);
        var postType = _metaModelTypesLocator.GetPostTypeByName(request.Type);

        var updateQuery = request.ToQuery(_requestContext.User.Id, postType.MetaFields.ToDictionary(s => s.Id));

        return updateQuery;
    }

    // ----------- OLD

    //public UserActionResult ImportData(string postTypeName, JArray json)
    //{
    //    throw new NotImplementedException();
    //    IServiceProvider _serviceProvider = default!;
    //    try
    //    {
    //        MarsDbContextLegacy ef = _serviceProvider.GetRequiredService<MarsDbContextLegacy>();

    //        PostType postType = ef.PostTypes.Include(s => s.MetaFields).FirstOrDefault(s => s.TypeName == postTypeName);

    //        FormService formService = _serviceProvider.GetRequiredService<FormService>();

    //        List<Post> posts = new List<Post>();

    //        foreach (JObject item in json)
    //        {

    //            Post post = formService.ParseJsonToPost(postType, item);
    //            foreach (var f in post.MetaValues)
    //            {
    //                f.MetaField = null;
    //            }
    //            posts.Add(post);
    //        }

    //        ef.Posts.AddRange(posts);

    //        throw new NotImplementedException();
    //        //compare option field

    //        //await ef.SaveChangesAsync();

    //        //return new UserActionResult
    //        //{
    //        //    Ok = true,
    //        //    Message = "import success"
    //        //};
    //    }
    //    catch (Exception ex)
    //    {
    //        return new UserActionResult
    //        {
    //            Ok = false,
    //            Message = ex.Message,
    //        };
    //    }

    //}

}
