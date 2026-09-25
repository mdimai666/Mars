using System.ComponentModel.DataAnnotations;
using Mars.Admin.Framework.Components.MetaFieldViews;
using Mars.Admin.Pages.PostTypeViews;
using Mars.Cms.Contracts.Posts;
using Mars.Cms.Contracts.PostTypes;
using Mars.Contracts.Models.Interfaces;
using Mars.Contracts.Resources;
using Mars.Core.Exceptions;
using Mars.Core.Extensions;
using Mars.Core.Features;
using Mars.Forms.Contracts;
using Mars.WebApiClient.Interfaces;

namespace Mars.Admin.Pages.PostsViews;

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

    [ValidateComplexType]
    public List<MetaValueEditModel> MetaValues { get; set; } = [];
    public Guid[] CategoryIds { get; set; } = [];

    /// <summary>
    /// Определение формы редактирования от сервера (дерево контейнеров с дескрипторами полей).
    /// Подменяется на месте после сохранения раскладки типа — несохранённые правки поста остаются.
    /// </summary>
    public FormDefinition? Form { get; set; }

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
    // Форма (общий слой Mars.Forms)

    /// <summary>Авто-подстановка slug из заголовка, пока slug пустой или похож на Guid</summary>
    public void AutoFillSlug()
    {
        if (string.IsNullOrWhiteSpace(Slug) || Guid.TryParse(Slug, out _))
            Slug = TextTool.TranslateToPostSlug(Title);
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
        => ToModel(vm.Post, vm.PostType, vm.Form);

    public static PostEditModel ToModel(PostEditResponse response, PostTypeDetailResponse postType, FormDefinition? form = null)
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
            Form = form,

            //extra
            PostType = PostTypeEditModel.ToModel(postType, [])
        };
}
