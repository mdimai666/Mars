using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using Mars.Admin.Framework.Components.MetaFieldViews;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostTypes;
using Mars.Contracts.Models.Interfaces;
using Mars.Contracts.Resources;
using Mars.Core.Attributes;
using Mars.Core.Exceptions;
using Mars.Forms.Contracts;
using Mars.WebApiClient.Interfaces;

namespace Mars.Admin.Pages.PostTypeViews;

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

    [Display(Name = nameof(AppRes.MetaFields), ResourceType = typeof(AppRes))]
    [ValidateComplexType]
    public List<MetaFieldEditModel> MetaFields { get; set; } = [];

    /// <summary>Параметры системных полей (правила, редактор) — <c>post_types.Options["systemFields"]</c></summary>
    public List<FormFieldSettings> SystemFields { get; set; } = [];

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
    /// Контент — системный слот: фича только открывает его, полей не создаёт.
    /// </summary>
    public void ToggleFeature(string feature, bool enabled)
    {
        if (enabled) EnabledFeatures.Add(feature);
        else EnabledFeatures.Remove(feature);

        // состав системных слотов зависит от фич — определения полей пересобираются
        InvalidateSystemFieldDefinitions();

        if (feature == PostTypeConstants.Features.PostImage && !enabled)
            ImageFieldKey = null;
    }

    List<FormFieldDefinition>? _systemFieldDefinitions;

    /// <summary>
    /// Определения системных полей для общего редактора: слоты каталога, включённые фичами типа,
    /// плюс сохранённые параметры. Каталог и признаки обязательности/чтения — те же, что на сервере
    /// (<see cref="SystemFieldsCatalog"/>), поэтому строка редактора показывает поле как в форме.
    /// </summary>
    public IReadOnlyList<FormFieldDefinition> SystemFieldDefinitions()
        => _systemFieldDefinitions ??= BuildSystemFieldDefinitions();

    public void InvalidateSystemFieldDefinitions() => _systemFieldDefinitions = null;

    List<FormFieldDefinition> BuildSystemFieldDefinitions()
    {
        var definitions = new List<FormFieldDefinition>();

        foreach (var slot in SystemFieldsCatalog.All)
        {
            if (slot.Feature is not null && !EnabledFeatures.Contains(slot.Feature)) continue;

            var settings = SystemFieldSettings(slot.Key);
            definitions.Add(new FormFieldDefinition
            {
                Key = slot.Key,
                TitleKey = slot.TitleKey,
                Type = slot.Type,
                Required = SystemFieldsCatalog.IsRequired(slot),
                ReadOnly = SystemFieldsCatalog.IsReadOnly(slot, EnabledFeatures),
                Multiple = slot.Multiple,
                ModelName = slot.ModelName,
                Zone = slot.Zone,
                Feature = slot.Feature,
                Editor = settings?.Editor ?? slot.Editor,
                Rules = settings?.Rules.ToList() ?? [],
                Source = this,
            });
        }

        return definitions;
    }

    /// <summary>Параметры системного слота типа (null — не заданы)</summary>
    public FormFieldSettings? SystemFieldSettings(string key)
        => SystemFields.FirstOrDefault(s => s.Key == key);

    /// <summary>Правка определения системного поля → параметры типа; пустые параметры не храним</summary>
    public void ApplySystemFieldDefinition(FormFieldDefinition definition)
    {
        var existing = SystemFieldSettings(definition.Key);
        SystemFields.RemoveAll(s => s.Key == definition.Key);

        var settings = new FormFieldSettings
        {
            Key = definition.Key,
            Editor = definition.Editor,
            CodeLang = existing?.CodeLang,
            Rules = definition.Rules.ToList(),
        };

        if (string.IsNullOrEmpty(settings.Editor) && string.IsNullOrEmpty(settings.CodeLang) && settings.Rules.Count == 0) return;

        SystemFields.Add(settings);
    }

    /// <summary>Язык редактора кода слота (панель настроек контента)</summary>
    public void SetSystemFieldCodeLang(string key, string codeLang)
    {
        var existing = SystemFieldSettings(key);
        SystemFields.RemoveAll(s => s.Key == key);

        SystemFields.Add(new FormFieldSettings
        {
            Key = key,
            Editor = existing?.Editor,
            CodeLang = string.IsNullOrEmpty(codeLang) ? null : codeLang,
            Rules = existing?.Rules.ToList() ?? [],
        });
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

    /// <summary>Ключ редактора контента (пусто = обычный многострочный текст)</summary>
    public string ContentEditorKey() => SystemFieldsCatalog.EditorKey(SystemFieldsCatalog.Content, SystemFields);

    /// <summary>Язык кода редактора контента</summary>
    public string ContentCodeLang() => SystemFieldsCatalog.CodeLang(SystemFieldsCatalog.Content, SystemFields);

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
            PostStatusList = PostStatusList.Select(s => s.ToCreateRequest()).ToList(),
            Tags = Tags,
            MetaFields = MetaFields.Select(s => s.ToCreateRequest()).ToList(),
            SystemFields = SystemFields,
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
            PostStatusList = PostStatusList.Select(s => s.ToUpdateRequest()).ToList(),
            Tags = Tags,
            MetaFields = MetaFields.Select(s => s.ToUpdateRequest()).ToList(),
            SystemFields = SystemFields,

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
            PostStatusList = response.PostStatusList.Select(PostStatusEditModel.ToModel).ToList(),
            Tags = response.Tags.ToArray(),
            MetaFields = response.MetaFields.Select(MetaFieldEditModel.ToModel).ToList(),
            SystemFields = response.SystemFields?.ToList() ?? [],

            MetaRelationModels = metaRelationModels,
        };
}
