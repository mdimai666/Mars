using System.ComponentModel.DataAnnotations;
using AppAdmin.Pages.PostTypeViews;
using AppFront.Shared.Components.MetaFieldViews;
using Mars.Core.Exceptions;
using Mars.Core.Extensions;
using Mars.Shared.Contracts.Posts;
using Mars.Shared.Contracts.PostTypes;
using Mars.Shared.Models.Interfaces;
using Mars.Shared.Resources;
using Mars.WebApiClient.Interfaces;

namespace AppAdmin.Pages.PostsViews;

/// <summary>
/// <see cref="PostEditResponse"/>
/// <see cref="PostEditViewModel"/>
/// </summary>
public class PostEditModel : IBasicEntity
{
    [Display(Name = nameof(AppRes.Id), ResourceType = typeof(AppRes))]
    public Guid Id { get; set; }

    [Display(Name = nameof(AppRes.CreationDate), ResourceType = typeof(AppRes))]
    public DateTimeOffset CreatedAt { get; set; }

    [Display(Name = nameof(AppRes.DateModified), ResourceType = typeof(AppRes))]
    public DateTimeOffset? ModifiedAt { get; init; }

    [Display(Name = nameof(AppRes.Title), ResourceType = typeof(AppRes))]
    public string Title { get; set; } = "";

    [StringLength(100)]
    [Display(Name = nameof(AppRes.PostType), ResourceType = typeof(AppRes))]
    public string Type { get; init; } = "";

    [StringLength(100)]
    [Display(Name = nameof(AppRes.Slug), ResourceType = typeof(AppRes))]
    public string Slug { get; set; } = "";

    [Display(Name = nameof(AppRes.Status), ResourceType = typeof(AppRes))]
    public string Status { get; set; } = "";

    [Display(Name = nameof(AppRes.Language), ResourceType = typeof(AppRes))]
    public string LangCode { get; set; } = "";

    [Display(Name = nameof(AppRes.Author), ResourceType = typeof(AppRes))]
    public PostAuthorResponse? Author { get; init; }

    public Guid UserId { get; set; }

    [Display(Name = nameof(AppRes.Content), ResourceType = typeof(AppRes))]
    public string Content { get; set; } = "";

    [Display(Name = nameof(AppRes.Excerpt), ResourceType = typeof(AppRes))]
    public string Excerpt { get; set; } = "";

    [Display(Name = nameof(AppRes.Tags), ResourceType = typeof(AppRes))]
    public string[] Tags { get; set; } = [];
    public List<MetaValueEditModel> MetaValues { get; set; } = new();
    public Guid[] CategoryIds { get; set; } = [];

    //==========================================
    //Internal

    public PostTypeEditModel PostType { get; init; } = new();

    /// <summary>
    /// <see cref="PostTypeConstants.Features"/>
    /// </summary>
    public bool FeatureActivated(string featureName)
    {
        return PostType.EnabledFeatures.Contains(featureName);
    }

    //==========================================
    //Backend

    public static async Task<PostEditModel> GetAction(IMarsWebApiClient client, Guid id, string postTypeName)
    {
        if (id == Guid.Empty)
        {
            ArgumentException.ThrowIfNullOrEmpty(postTypeName, nameof(postTypeName));
            var vm = await client.Post.GetPostBlank(postTypeName);
            return FromViewModel(vm);
        }
        else
        {
            var vm = await client.Post.GetEditModel(id) ?? throw new NotFoundException();
            return FromViewModel(vm);
        }
    }

    public static async Task<PostEditModel> SaveAction(IMarsWebApiClient client, PostEditModel post, bool isNew)
    {
        if (isNew)
        {
            var created = await client.Post.Create(post.ToCreateRequest());
            post.Id = created.Id;
        }
        else
        {
            await client.Post.Update(post.ToUpdateRequest());
        }
        return post;
    }

    public static Task DeleteAction(IMarsWebApiClient client, PostEditModel postType)
    {
        return client.Post.Delete(postType.Id);
    }

    public CreatePostRequest ToCreateRequest()
        => new()
        {
            Id = Id,
            Title = Title,
            Slug = Slug,
            LangCode = LangCode,
            Content = Content,
            Excerpt = Excerpt.AsNullIfEmpty(),
            Tags = Tags,
            Status = Status,
            Type = Type,
            MetaValues = MetaValues.Select(s => s.ToCreateRequest()).ToList(),
            CategoryIds = CategoryIds,
        };

    public UpdatePostRequest ToUpdateRequest()
        => new()
        {
            Id = Id,
            Title = Title,
            Slug = Slug,
            LangCode = LangCode,
            Content = Content,
            Excerpt = Excerpt.AsNullIfEmpty(),
            Tags = Tags,
            Status = Status,
            Type = Type,
            MetaValues = MetaValues.Select(s => s.ToUpdateRequest()).ToList(),
            CategoryIds = CategoryIds,
        };

    public static PostEditModel FromViewModel(PostEditViewModel vm)
        => ToModel(vm.Post, vm.PostType);

    public static PostEditModel ToModel(PostEditResponse response, PostTypeDetailResponse postType)
        => new()
        {
            Id = response.Id,
            CreatedAt = response.CreatedAt,
            ModifiedAt = response.ModifiedAt,
            Title = response.Title,
            Slug = response.Slug,
            Type = response.Type,
            Tags = response.Tags.ToArray(),
            Content = response.Content ?? "",
            Excerpt = response.Excerpt ?? "",
            LangCode = response.LangCode,
            Status = response.Status,
            Author = response.Author,
            UserId = response.Author.Id,
            MetaValues = response.MetaValues.Select(MetaValueEditModel.ToModel).ToList(),
            CategoryIds = response.CategoryIds.ToArray(),

            //extra
            PostType = PostTypeEditModel.ToModel(postType, [])
        };
}
