using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Mars.Core.Extensions;
using Mars.Host.Exceptions;
using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Mappings.Posts;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Validators;
using Mars.Host.Templators;
using Mars.Shared.Common;
using Microsoft.EntityFrameworkCore;

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



    // ==========================
    // OLD
    // ==========================

    //public async Task<JToken> GetAsJson(Expression<Func<Post, bool>> predicate)
    //{
    //    throw new NotImplementedException();
        //MarsDbContextLegacy ef = _serviceProvider.GetRequiredService<MarsDbContextLegacy>();

        //AppShared.Dto.UserRoleDto user = await GetCurrentUserWithRoles(ef);

        //var post = await ef.Posts
        //    .Include(s => s.User)
        //    .Include(s => s.MetaValues)
        //        .ThenInclude(s => s.MetaField)
        //    .FirstOrDefaultAsync(predicate);

        //var postType = await ef.PostTypes.Include(s => s.MetaFields).FirstOrDefaultAsync(s => s.TypeName == post.Type);

        ////List<MetaField> userMetafields = await userService.UserMetaFields(ef);
        //List<MetaField> userMetafieldsEmpty = new List<MetaField>();

        //MfPreparePostContext pctx3 = new MfPreparePostContext
        //{
        //    ef = ef,
        //    post = post,
        //    postType = postType,
        //    user = user,
        //    userMetaFields = userMetafieldsEmpty,

        //};

        //var _postJson = AsJson2(pctx3);

        //var meta = post.MetaValues.Select(s => s.MetaField);
        //List<JsonPostMetaInfo> metaInfo = new();

        //foreach (var a in meta)
        //{
        //    MetaField parent = null;
        //    if (a.ParentId != Guid.Empty) parent = meta.FirstOrDefault(s => s.Id == a.ParentId);

        //    metaInfo.Add(new JsonPostMetaInfo
        //    {
        //        Key = a.Key,
        //        Title = a.Title,
        //        Type = a.Type,
        //        Parent = parent?.Key ?? ""
        //    });
        //}


        //_postJson.Add("_meta", JToken.FromObject(metaInfo));

        //return _postJson;
    //}


    //public async Task<TotalResponse<JToken>> ListTableJson(QueryFilter filter, string type, Expression<Func<Post, bool>> predicate = null)
    //{
    //    throw new NotImplementedException();
        //MarsDbContextLegacy ef = _serviceProvider.GetRequiredService<MarsDbContextLegacy>();

        //var postType = await ef.PostTypes.Include(s => s.MetaFields).FirstOrDefaultAsync(s => s.TypeName == type);

        //if (postType == null)
        //{
        //    return new TotalResponse<JToken>
        //    {
        //        Result = ETotalResponeResult.ERROR,
        //        Message = "Post type not found"
        //    };
        //}

        //UserService userService = _serviceProvider.GetRequiredService<UserService>();

        ////List<MetaField> userMetafields = await userService.UserMetaFields(ef);
        //List<MetaField> userMetafieldsEmpty = new List<MetaField>();

        //AppShared.Dto.UserRoleDto user = await GetCurrentUserWithRoles(ef);

        //var query = ef.Posts
        //    .Include(s => s.User)
        //    .Include(s => s.MetaValues)
        //        .ThenInclude(s => s.MetaField)
        //    .Where(s => s.Type == type)
        //    .AsQueryable();

        //if (predicate is not null)
        //{
        //    query = query.Where(predicate);
        //}

        //var posts = await query.QueryTable(filter);

        //JArray arr = new JArray();

        //foreach (var post in posts.Records)
        //{

        //    MfPreparePostContext pctx3 = new MfPreparePostContext
        //    {
        //        ef = ef,
        //        post = post,
        //        postType = postType,
        //        user = user,
        //        userMetaFields = userMetafieldsEmpty,

        //    };

        //    //string _postJson = await postService.AsJson2(pctx3);
        //    var _postJson = AsJson2(pctx3);

        //    arr.Add(_postJson);
        //}

        //return new TotalResponse<JToken>
        //{
        //    Message = posts.Message,
        //    Result = posts.Result,
        //    Records = arr,
        //    TotalCount = posts.TotalCount
        //};
    //}

    //public JObject AsJson22(object pctx) => AsJson2(pctx as MfPreparePostContext);
    //public JObject AsJson2(MfPreparePostContext pctx)
    //{
    //    throw new NotImplementedException();

    //    //IServiceProvider _serviceProvider = default!;

    //    //if (pctx.post == null) throw new PageNotFoundException();

    //    //var postDto = new PostDto(pctx.post);

    //    //JsonSerializerOptions opt = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    //    //{
    //    //    MaxDepth = 0,
    //    //    //IgnoreReadOnlyProperties = true,
    //    //    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
    //    //    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    //    //};


    //    //if (pctx.postType.EnabledFeatures.Contains(nameof(Post.Content)) == false)
    //    //{
    //    //    postDto.Content = null;
    //    //}

    //    //if (pctx.postType.EnabledFeatures.Contains(nameof(Post.Likes)) == true)
    //    //{
    //    //    if (pctx.user is not null)
    //    //    {
    //    //        postDto.IsLiked = pctx.ef.Posts.Include(s => s.Likes).Count(s => s.Id == pctx.post.Id && s.Likes.Select(s => s.UserId).Contains(pctx.user.Id)) > 0;
    //    //    }
    //    //}

    //    ////var postDtoJson = System.Text.Json.JsonSerializer.Serialize(postDto, opt);

    //    //JObject jPost = JObject.FromObject(postDto);

    //    //var meOpt = new JsonMergeSettings
    //    //{
    //    //    // union array values together to avoid duplicates
    //    //    MergeArrayHandling = MergeArrayHandling.Union,
    //    //};

    //    //pctx.post.MetaValues = MetaField.GetValuesBlank(pctx.post.MetaValues, pctx.postType.MetaFields);

    //    //var meta = MfPreparePostContext.AsJson2(ref pctx, pctx.post.MetaValues, pctx.postType.MetaFields, _serviceProvider);

    //    ////var post2 = JsonMergeExtensions.Merge(postDtoJson, meta);
    //    //jPost.Merge(JObject.Parse(meta.ToJsonString(opt)), meOpt);
    //    ////foreach (var f in meta)
    //    ////{
    //    ////    jPost.Add(f.Key, JToken.FromObject(f.Value));
    //    ////}

    //    //JsonObject userMeta;

    //    //if (postDto.User is not null)
    //    //{
    //    //    pctx.post.User.MetaValues ??= pctx.ef.Users
    //    //        .Include(s => s.MetaValues).ThenInclude(s => s.MetaField)
    //    //        .FirstOrDefault(s => s.Id == postDto.User.Id)?.MetaValues;
    //    //    userMeta = MfPreparePostContext.AsJson2(ref pctx, pctx.post.User.MetaValues, pctx.userMetaFields, _serviceProvider);
    //    //}
    //    //else
    //    //{
    //    //    userMeta = new JsonObject();
    //    //}

    //    ////post2 = JsonMergeExtensions.Merge(post2, userMeta);
    //    //JObject userDto2 = JObject.FromObject(postDto.User);
    //    //userDto2.Merge(JObject.Parse(userMeta.ToJsonString()), meOpt);
    //    ////foreach (var f in userMeta)
    //    ////{
    //    ////    userDto2.Add(f.Key, JToken.FromObject(f.Value));
    //    ////}

    //    //jPost.Remove(nameof(Post.User));
    //    //jPost.Add(nameof(Post.User), userDto2);


    //    //return jPost;

    //}

    //public async Task<string> AsJson(Guid id)
    //{
    //    return await AsJson(s => s.Id == id);
    //}

    //public async Task<string> AsJson(Expression<Func<Post, bool>> exp)
    //{
    //    throw new NotImplementedException();
    //    //var post = await Get(exp);
    //    //if (post == null) throw new PageNotFoundException();

    //    //MarsDbContextLegacy ef = _serviceProvider.GetRequiredService<MarsDbContextLegacy>();
    //    //PostType postType = ef.PostTypes.Include(s => s.MetaFields).FirstOrDefault(s => s.TypeName == post.Type);

    //    //return AsJson(post, postType);
    //}


    /// <summary>
    /// 
    /// </summary>
    /// <param name="post"></param>
    /// <param name="postType"></param>
    /// <returns></returns>
    /// <exception cref="PageNotFoundException"></exception>
    public string AsJson(PostDetail post)
    {
        throw new NotImplementedException();

        //if (post == null) throw new PageNotFoundException();



        //var postDto = new PostDto(post);


        //JsonSerializerOptions opt = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        //{
        //    MaxDepth = 0,
        //    //IgnoreReadOnlyProperties = true,
        //    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,

        //};

        ////var doc = System.Text.Json.JsonSerializer.SerializeToNode(postDto, opt);

        ////var j = doc.AsObject();

        ////var tree = MetaValue.AsJsonObject(post.MetaValues, postType.MetaFields);

        ////j.Remove(nameof(AppShared.Models.Post.MetaValues));

        ////j.add

        ////foreach (var a in tree)
        ////{
        ////    if (j.ContainsKey(a.Key) == false)
        ////    {
        ////        j.Add(a.Key, a.Value.);
        ////    }
        ////}

        //////j.Add("meta", tree);
        ////return j.ToJsonString();

        //if (postType.EnabledFeatures.Contains(nameof(Post.Content)) == false)
        //{
        //    postDto.Content = null;
        //}

        //var postDtoJson = JsonSerializer.Serialize(postDto, opt);

        //var meta = MetaValue.AsJson(post.MetaValues, postType.MetaFields);

        //return JsonMergeExtensions.Merge(postDtoJson, meta);
    }

}
