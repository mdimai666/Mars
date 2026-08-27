using System.ComponentModel.DataAnnotations;
using AppFront.Shared.Components.MetaFieldViews;
using Mars.Core.Attributes;
using Mars.Core.Exceptions;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.PostCategoryTypes;
using Mars.Shared.Models.Interfaces;
using Mars.Shared.Resources;
using Mars.WebApiClient.Interfaces;

namespace AppAdmin.Pages.PostCategoryTypeViews;

/// <summary>
/// <see cref="PostCategoryTypeDetailResponse"/>
/// </summary>
public class PostCategoryTypeEditModel : IBasicEntity
{
    [Display(Name = nameof(AppRes.Id), ResourceType = typeof(AppRes))]
    public Guid Id { get; set; }

    [Display(Name = nameof(AppRes.CreationDate), ResourceType = typeof(AppRes))]
    public DateTimeOffset CreatedAt { get; init; }

    [Display(Name = nameof(AppRes.DateModified), ResourceType = typeof(AppRes))]
    public DateTimeOffset? ModifiedAt { get; init; }

    [Required(ErrorMessageResourceName = nameof(AppRes.Title), ErrorMessageResourceType = typeof(AppRes))]
    public string Title { get; set; } = "";

    [Display(Name = nameof(AppRes.TypeName), ResourceType = typeof(AppRes))]
    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [StringLength(50, MinimumLength = 3)]
    [SlugString]
    public string TypeName { get; set; } = "";

    [Display(Name = nameof(AppRes.Tags), ResourceType = typeof(AppRes))]
    public string[] Tags { get; set; } = [];

    [Display(Name = nameof(AppRes.MetaFields), ResourceType = typeof(AppRes))]
    [ValidateComplexType]
    public List<MetaFieldEditModel> MetaFields { get; set; } = [];

    //==========================================
    //Internal

    public IReadOnlyCollection<MetaRelationModelResponse> MetaRelationModels { get; set; } = [];

    //==========================================
    //Backend

    public static async Task<PostCategoryTypeEditModel> GetAction(IMarsWebApiClient client, Guid id)
    {
        if (id == Guid.Empty)
        {
            var metaRelationModels = await client.PostType.AllMetaRelationsStructure();
            return new()
            {
                MetaRelationModels = metaRelationModels,
            };
        }
        else
        {
            var vm = await client.PostCategoryType.GetEditModel(id) ?? throw new NotFoundException();
            return FromViewModel(vm);
        }

    }

    public static async Task<PostCategoryTypeEditModel> SaveAction(IMarsWebApiClient client, PostCategoryTypeEditModel postType, bool isNew)
    {
        if (isNew)
        {
            var created = await client.PostCategoryType.Create(postType.ToCreateRequest());
            postType.Id = created.Id;
        }
        else
        {
            await client.PostCategoryType.Update(postType.ToUpdateRequest());
        }
        return postType;
    }

    public static Task DeleteAction(IMarsWebApiClient client, PostCategoryTypeEditModel postType)
    {
        return client.PostCategoryType.Delete(postType.Id);
    }

    public CreatePostCategoryTypeRequest ToCreateRequest()
        => new()
        {
            Id = Id,
            Title = Title,
            TypeName = TypeName,
            Tags = Tags,
            MetaFields = MetaFields.Select(s => s.ToCreateRequest()).ToList(),
        };

    public UpdatePostCategoryTypeRequest ToUpdateRequest()
        => new()
        {
            Id = Id,
            Title = Title,
            TypeName = TypeName,
            Tags = Tags,
            MetaFields = MetaFields.Select(s => s.ToUpdateRequest()).ToList(),

        };

    public static PostCategoryTypeEditModel FromViewModel(PostCategoryTypeEditViewModel vm)
        => ToModel(vm.PostCategoryType, vm.MetaRelationModels);

    public static PostCategoryTypeEditModel ToModel(PostCategoryTypeDetailResponse response, IReadOnlyCollection<MetaRelationModelResponse> metaRelationModels)
        => new()
        {
            Id = response.Id,
            Title = response.Title,
            CreatedAt = response.CreatedAt,
            ModifiedAt = response.ModifiedAt,
            TypeName = response.TypeName,
            Tags = response.Tags.ToArray(),
            MetaFields = response.MetaFields.Select(MetaFieldEditModel.ToModel).ToList(),

            MetaRelationModels = metaRelationModels,
        };
}
