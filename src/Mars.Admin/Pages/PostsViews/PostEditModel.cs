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

    Dictionary<(string Key, int Index), MetaValueEditModel>? _metaValuesByIndex;

    /// <summary>
    /// Доступ к значениям по <c>(ключ поля, Index)</c>; одиночные значения — <c>(key, 0)</c>.
    /// Кэш по текущему списку <see cref="MetaValues"/>: пересобирается при изменении состава списка.
    /// </summary>
    public IReadOnlyDictionary<(string Key, int Index), MetaValueEditModel> MetaValuesByIndex
    {
        get
        {
            if (_metaValuesByIndex is null || _metaValuesByIndex.Count != MetaValues.Count)
            {
                _metaValuesByIndex = MetaValues.ToKeyIndexDictionary();
            }
            return _metaValuesByIndex;
        }
    }

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

    /// <summary>
    /// Мешок значений формы для системных слотов (<see cref="SystemFieldsCatalog"/>).
    /// Метаполя остаются в <see cref="MetaValues"/>, контент — в <see cref="Content"/>:
    /// транспорт поста типизированный, мешок живёт только внутри формы.
    /// </summary>
    public FormValues BuildFormValues(string ownerModel)
    {
        var values = new FormValues { OwnerModel = ownerModel };
        FillFormValues(values);
        return values;
    }

    /// <summary>Обновить мешок из модели — после внешних изменений (ИИ-агент, авто-подстановка slug)</summary>
    public void FillFormValues(FormValues values)
    {
        values.Values[SystemFieldsCatalog.Title] = FormValueCodec.FromClr(Title, FormFieldType.String);
        values.Values[SystemFieldsCatalog.Slug] = FormValueCodec.FromClr(Slug, FormFieldType.String);
        values.Values[SystemFieldsCatalog.Excerpt] = FormValueCodec.FromClr(Excerpt, FormFieldType.Text);
        values.Values[SystemFieldsCatalog.Status] = FormValueCodec.FromClr(Status, FormFieldType.Select);
        values.Values[SystemFieldsCatalog.Lang] = FormValueCodec.FromClr(LangCode, FormFieldType.String);
        values.Values[SystemFieldsCatalog.CreatedAt] = FormValueCodec.FromClr(CreatedAt, FormFieldType.DateTime);
        values.Values[SystemFieldsCatalog.ModifiedAt] = FormValueCodec.FromClr(ModifiedAt, FormFieldType.DateTime);
        values.Values[SystemFieldsCatalog.Author] =
            FormValueCodec.FromClr(UserId == Guid.Empty ? null : UserId, FormFieldType.Relation);
        values.Values[SystemFieldsCatalog.Tags] = FormValueCodec.FromClrList(Tags, FormFieldType.String);
        values.Values[SystemFieldsCatalog.Categories] = FormValueCodec.FromClrList(CategoryIds, FormFieldType.Relation);
    }

    /// <summary>Разобрать мешок в типизированные свойства; <see cref="ModifiedAt"/> и автор — только чтение</summary>
    public void ApplyFormValues(FormValues values)
    {
        if (TryRead(values, SystemFieldsCatalog.Title, FormFieldType.String, out var title))
        {
            var text = title as string ?? "";
            if (Title != text)
            {
                Title = text;
                AutoFillSlug();
            }
        }
        if (TryRead(values, SystemFieldsCatalog.Slug, FormFieldType.String, out var slug)) Slug = slug as string ?? "";
        if (TryRead(values, SystemFieldsCatalog.Excerpt, FormFieldType.Text, out var excerpt)) Excerpt = excerpt as string ?? "";
        if (TryRead(values, SystemFieldsCatalog.Status, FormFieldType.Select, out var status)) Status = status as string ?? "";
        if (TryRead(values, SystemFieldsCatalog.Lang, FormFieldType.String, out var lang)) LangCode = lang as string ?? "";
        if (TryRead(values, SystemFieldsCatalog.CreatedAt, FormFieldType.DateTime, out var created)
            && created is DateTimeOffset createdAt) CreatedAt = createdAt;
        if (TryReadList(values, SystemFieldsCatalog.Tags, FormFieldType.String, out var tags))
            Tags = tags.OfType<string>().ToArray();
        if (TryReadList(values, SystemFieldsCatalog.Categories, FormFieldType.Relation, out var categories))
            CategoryIds = categories.OfType<Guid>().ToArray();

        static bool TryRead(FormValues values, string key, FormFieldType type, out object? value)
        {
            value = null;
            return values.Has(key) && FormValueCodec.TryToClr(values.Value(key), type, out value, out _);
        }

        static bool TryReadList(FormValues values, string key, FormFieldType type, out IReadOnlyList<object?> list)
        {
            list = [];
            return values.Has(key) && FormValueCodec.TryToClrList(values.Value(key), type, out list, out _);
        }
    }

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
