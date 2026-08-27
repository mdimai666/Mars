using System.ComponentModel.DataAnnotations;
using AppAdmin.Pages.PostCategoryTypeViews;
using AppFront.Shared.Components.MetaFieldViews;
using Mars.Core.Exceptions;
using Mars.Shared.Contracts.PostCategories;
using Mars.Shared.Contracts.PostCategoryTypes;
using Mars.Shared.Models.Interfaces;
using Mars.Shared.Resources;
using Mars.WebApiClient.Interfaces;

namespace AppAdmin.Pages.PostCategoriesViews;

/// <summary>
/// <see cref="PostCategoryEditResponse"/>
/// <see cref="PostCategoryEditViewModel"/>
/// </summary>
public class PostCategoryEditModel : IBasicEntity
{
    [Display(Name = nameof(AppRes.Id), ResourceType = typeof(AppRes))]
    public Guid Id { get; set; }

    [Display(Name = nameof(AppRes.CreationDate), ResourceType = typeof(AppRes))]
    public DateTimeOffset CreatedAt { get; set; }

    [Display(Name = nameof(AppRes.DateModified), ResourceType = typeof(AppRes))]
    public DateTimeOffset? ModifiedAt { get; set; }

    [Display(Name = nameof(AppRes.Title), ResourceType = typeof(AppRes))]
    public string Title { get; set; } = "";

    [StringLength(100)]
    [Display(Name = nameof(AppRes.PostCategoryType), ResourceType = typeof(AppRes))]
    public string Type { get; set; } = "";

    [StringLength(100)]
    [Display(Name = nameof(AppRes.Slug), ResourceType = typeof(AppRes))]
    public string Slug { get; set; } = "";

    [Display(Name = nameof(AppRes.Tags), ResourceType = typeof(AppRes))]
    public string[] Tags { get; set; } = [];
    public List<MetaValueEditModel> MetaValues { get; set; } = [];

    //--

    [StringLength(100)]
    [Display(Name = nameof(AppRes.PostType), ResourceType = typeof(AppRes))]
    public string PostType { get; set; } = "";

    public Guid ParentId { get; set; }
    public Guid[] PathIds { get; set; } = [];
    public bool Disabled { get; set; }

    //==========================================
    //Internal

    public PostCategoryTypeEditModel PostCategoryType { get; set; } = new();

    //==========================================
    //Backend

    public static async Task<PostCategoryEditModel> GetAction(IMarsWebApiClient client, Guid id, string postCategoryTypeName, string postTypeName)
    {
        if (id == Guid.Empty)
        {
            ArgumentException.ThrowIfNullOrEmpty(postTypeName, nameof(postTypeName));
            var vm = await client.PostCategory.GetBlankModel(postCategoryTypeName, postTypeName);
            return FromViewModel(vm);
        }
        else
        {
            var vm = await client.PostCategory.GetEditModel(id) ?? throw new NotFoundException();
            return FromViewModel(vm);
        }
    }

    public static async Task<PostCategoryEditModel> SaveAction(IMarsWebApiClient client, PostCategoryEditModel post, bool isNew)
    {
        if (isNew)
        {
            var created = await client.PostCategory.Create(post.ToCreateRequest());
            post.Id = created.Id;
        }
        else
        {
            await client.PostCategory.Update(post.ToUpdateRequest());
        }
        return post;
    }

    public static Task DeleteAction(IMarsWebApiClient client, PostCategoryEditModel postType)
    {
        return client.PostCategory.Delete(postType.Id);
    }

    public CreatePostCategoryRequest ToCreateRequest()
        => new()
        {
            Id = Id,
            Title = Title,
            Slug = Slug,
            Tags = Tags,
            Type = Type,
            PostType = PostType,

            Disabled = Disabled,
            ParentId = ParentId == Guid.Empty ? null : ParentId,

            MetaValues = MetaValues.Select(s => s.ToCreateRequest()).ToList(),
        };

    public UpdatePostCategoryRequest ToUpdateRequest()
        => new()
        {
            Id = Id,
            Title = Title,
            Slug = Slug,
            Tags = Tags,
            Type = Type,
            PostType = PostType,

            Disabled = Disabled,
            ParentId = ParentId == Guid.Empty ? null : ParentId,

            MetaValues = MetaValues.Select(s => s.ToUpdateRequest()).ToList(),
        };

    public static PostCategoryEditModel FromViewModel(PostCategoryEditViewModel vm)
        => ToModel(vm.PostCategory, vm.PostCategoryType);

    public static PostCategoryEditModel ToModel(PostCategoryEditResponse response, PostCategoryTypeDetailResponse postType)
        => new()
        {
            Id = response.Id,
            CreatedAt = response.CreatedAt,
            ModifiedAt = response.ModifiedAt,
            Title = response.Title,
            Slug = response.Slug,
            Tags = response.Tags.ToArray(),
            Type = response.Type,
            PostType = response.PostType,

            Disabled = response.Disabled,
            ParentId = response.ParentId ?? Guid.Empty,

            MetaValues = response.MetaValues.Select(MetaValueEditModel.ToModel).ToList(),

            //extra
            PostCategoryType = PostCategoryTypeEditModel.ToModel(postType, [])
        };
}
