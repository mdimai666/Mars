using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using AppFront.Shared.Components.MetaFieldViews;
using Mars.Core.Attributes;
using Mars.Core.Exceptions;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.PostTypes;
using Mars.Shared.Models.Interfaces;
using Mars.Shared.Resources;
using Mars.WebApiClient.Interfaces;

namespace AppAdmin.Pages.PostTypeViews;

/// <summary>
/// <see cref="PostTypeDetailResponse"/>
/// </summary>
public class PostTypeEditModel : IBasicEntity
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

    [Display(Name = nameof(AppRes.EnabledFeatures), ResourceType = typeof(AppRes))]
    public List<string> EnabledFeatures { get; set; } = [];

    [Display(Name = nameof(AppRes.Statuses), ResourceType = typeof(AppRes))]
    [ValidateComplexType]
    public List<PostStatusEditModel> PostStatusList { get; set; } = [];

    public bool Disabled { get; set; }

    [Display(Name = "Видимость")]
    public PostTypeVisibility Visibility { get; set; } = PostTypeVisibility.Public;

    /// <summary>Поле картинки поста — указатель фичи <see cref="PostTypeConstants.Features.PostImage"/>;
    /// обязательно выбрано, пока фича включена</summary>
    [Display(Name = "Поле картинки поста")]
    public string? ImageFieldKey { get; set; }

    [ValidateComplexType]
    public PostContentSettingsEditModel PostContentSettings { get; set; } = new();

    [Display(Name = nameof(AppRes.MetaFields), ResourceType = typeof(AppRes))]
    [ValidateComplexType]
    public List<MetaFieldEditModel> MetaFields { get; set; } = [];

    //==========================================
    //Internal

    /// <summary>
    /// <see cref="PostTypeConstants.Features"/>
    /// </summary>
    public bool FeatureActivated(string featureName)
    {
        return EnabledFeatures.Contains(featureName);
    }

    /// <summary>
    /// Включение/выключение фичи. Для фич с требуемыми полями страница
    /// дополнительно оркестрирует выбор/создание поля-указателя
    /// (см. <see cref="CreateFeatureImageField"/>); серверная согласованность —
    /// на валидаторе (<c>UpdatePostTypeQueryValidator</c>).
    /// </summary>
    public void ToggleFeature(string feature, bool enabled)
    {
        if (enabled) EnabledFeatures.Add(feature);
        else EnabledFeatures.Remove(feature);

        if (feature == PostTypeConstants.Features.PostImage && !enabled)
            ImageFieldKey = null;
    }

    /// <summary>Переименование ключа поля: указатель картинки следует за полем, к которому привязан</summary>
    public void OnFieldKeyRenamed(string oldKey, string newKey)
    {
        if (ImageFieldKey == oldKey) ImageFieldKey = newKey;
    }

    /// <summary>Поля-изображения типа — кандидаты в указатель картинки</summary>
    public IReadOnlyCollection<MetaFieldEditModel> ImagePointerCandidates()
        => MetaFields.Where(f => f.Type == MetaFieldType.Image).ToList();

    /// <summary>Создаёт поле фичи «Картинка поста» и добавляет в поля типа</summary>
    public MetaFieldEditModel CreateFeatureImageField()
    {
        var takenKeys = MetaFields.Select(f => f.Key).ToHashSet(StringComparer.Ordinal);
        var key = FeatureFieldsCatalog.PostImageFieldKey;
        var suffix = 2;
        while (!takenKeys.Add(key))
        {
            key = $"{FeatureFieldsCatalog.PostImageFieldKey}_{suffix}";
            suffix++;
        }

        var field = new MetaFieldEditModel
        {
            Id = Guid.NewGuid(),
            Title = FeatureFieldsCatalog.PostImageFieldTitle,
            Key = key,
            Type = MetaFieldType.Image,
            IsNullable = true,
            IsNew = true,
            Order = MetaFields.Count == 0 ? 0 : MetaFields.Max(f => f.Order) + 1,
            Options = new JsonObject
            {
                [FeatureFieldsCatalog.FeatureKeyOption()] = FeatureFieldsCatalog.PostImage,
            },
        };

        MetaFields.Add(field);
        return field;
    }

    public IReadOnlyCollection<MetaRelationModelResponse> MetaRelationModels { get; set; } = [];

    //==========================================
    //Backend

    public static async Task<PostTypeEditModel> GetAction(IMarsWebApiClient client, Guid id)
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
            var vm = await client.PostType.GetEditModel(id) ?? throw new NotFoundException();
            return FromViewModel(vm);
        }

    }

    public static async Task<PostTypeEditModel> SaveAction(IMarsWebApiClient client, PostTypeEditModel postType, bool isNew)
    {
        if (isNew)
        {
            var created = await client.PostType.Create(postType.ToCreateRequest());
            postType.Id = created.Id;
        }
        else
        {
            await client.PostType.Update(postType.ToUpdateRequest());
        }
        return postType;
    }

    public static Task DeleteAction(IMarsWebApiClient client, PostTypeEditModel postType)
    {
        return client.PostType.Delete(postType.Id);
    }

    public CreatePostTypeRequest ToCreateRequest()
        => new()
        {
            Id = Id,
            Title = Title,
            TypeName = TypeName,
            EnabledFeatures = EnabledFeatures,
            Disabled = Disabled,
            Visibility = Visibility,
            ImageFieldKey = ImageFieldKey,
            PostContentSettings = PostContentSettings.ToCreateRequest(),
            PostStatusList = PostStatusList.Select(s => s.ToCreateRequest()).ToList(),
            Tags = Tags,
            MetaFields = MetaFields.Select(s => s.ToCreateRequest()).ToList(),
        };

    public UpdatePostTypeRequest ToUpdateRequest()
        => new()
        {
            Id = Id,
            Title = Title,
            TypeName = TypeName,
            EnabledFeatures = EnabledFeatures,
            Disabled = Disabled,
            Visibility = Visibility,
            ImageFieldKey = ImageFieldKey,
            PostContentSettings = PostContentSettings.ToUpdateRequest(),
            PostStatusList = PostStatusList.Select(s => s.ToUpdateRequest()).ToList(),
            Tags = Tags,
            MetaFields = MetaFields.Select(s => s.ToUpdateRequest()).ToList(),

        };

    public static PostTypeEditModel FromViewModel(PostTypeEditViewModel vm)
        => ToModel(vm.PostType, vm.MetaRelationModels);

    public static PostTypeEditModel ToModel(PostTypeDetailResponse response, IReadOnlyCollection<MetaRelationModelResponse> metaRelationModels)
        => new()
        {
            Id = response.Id,
            Title = response.Title,
            CreatedAt = response.CreatedAt,
            ModifiedAt = response.ModifiedAt,
            TypeName = response.TypeName,
            EnabledFeatures = response.EnabledFeatures.ToList(),
            Disabled = response.Disabled,
            Visibility = response.Visibility,
            ImageFieldKey = response.ImageFieldKey,
            PostContentSettings = PostContentSettingsEditModel.ToModel(response.PostContentSettings),
            PostStatusList = response.PostStatusList.Select(PostStatusEditModel.ToModel).ToList(),
            Tags = response.Tags.ToArray(),
            MetaFields = response.MetaFields.Select(MetaFieldEditModel.ToModel).ToList(),

            MetaRelationModels = metaRelationModels,
        };
}
