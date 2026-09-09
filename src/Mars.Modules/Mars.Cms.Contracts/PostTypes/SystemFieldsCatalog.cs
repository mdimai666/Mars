using Mars.Contracts.Resources;
using Mars.Forms.Contracts;

namespace Mars.Cms.Contracts.PostTypes;

/// <summary>
/// Каталог системных слотов формы поста: поля, которые живут в типизированных колонках <c>posts</c>
/// (и его связках), а не в <c>post_meta_values</c>. Метаполя сюда не входят — они производят
/// дескрипторы сами. Ключи совпадают с ключами базовых колонок грида
/// (<see cref="PostTypeGridConstants"/>), поэтому грид и форма делят одно пространство ключей;
/// состав и порядок колонок грида — в <see cref="PostTypeGridColumns"/>.
/// Порядок списка — порядок по умолчанию в форме поста.
/// </summary>
public static class SystemFieldsCatalog
{
    public const string Title = "title";
    public const string Slug = "slug";
    public const string Excerpt = "excerpt";
    public const string Status = "status";
    public const string CreatedAt = "created_at";
    public const string ModifiedAt = "modified_at";
    public const string Lang = "lang";
    public const string Author = "author";
    public const string Categories = "categories";
    public const string Tags = "tags";

    /// <summary>Зоны формы поста — 1:1 с render-фрагментами <c>StandardEditContainer</c></summary>
    public static class Zones
    {
        /// <summary>Основная колонка</summary>
        public const string Main = "main";

        /// <summary>Карточка публикации (сайдбар)</summary>
        public const string Publish = "publish";

        /// <summary>Дополнительный сайдбар (таксономии)</summary>
        public const string Extra = "extra";

        public static readonly IReadOnlyList<FormZoneDescriptor> All =
        [
            new FormZoneDescriptor { Key = Main, Title = "Основное" },
            new FormZoneDescriptor { Key = Publish, Title = "Публикация" },
            new FormZoneDescriptor { Key = Extra, Title = "Дополнительно" },
        ];
    }

    /// <summary>
    /// Слот системного поля. <see cref="TitleKey"/> — ключ ресурса <see cref="AppRes"/>;
    /// <see cref="Feature"/> — фича типа, которая включает слот (null — всегда доступен);
    /// <see cref="Editor"/> — ключ доменного редактора (null — встроенный редактор типа).
    /// </summary>
    public sealed record SystemFieldSlot(string Key, string TitleKey, FormFieldType Type, string Zone,
                                         string? Feature = null, bool Multiple = false, bool ReadOnly = false,
                                         string? Editor = null, string? ModelName = null);

    public static readonly IReadOnlyList<SystemFieldSlot> All =
    [
        new(Title, nameof(AppRes.Title), FormFieldType.String, Zones.Main,
            Editor: PostFormEditors.Title),
        new(Slug, nameof(AppRes.Slug), FormFieldType.String, Zones.Main),
        new(Excerpt, nameof(AppRes.Excerpt), FormFieldType.Text, Zones.Main,
            Feature: PostTypeConstants.Features.Excerpt),
        new(CreatedAt, nameof(AppRes.CreatedAt), FormFieldType.DateTime, Zones.Publish),
        new(ModifiedAt, nameof(AppRes.DateModified), FormFieldType.DateTime, Zones.Publish, ReadOnly: true),
        new(Status, nameof(AppRes.Status), FormFieldType.Select, Zones.Publish,
            Feature: PostTypeConstants.Features.Status, Editor: PostFormEditors.Status),
        new(Lang, nameof(AppRes.Language), FormFieldType.String, Zones.Publish,
            Feature: PostTypeConstants.Features.Language),
        new(Author, nameof(AppRes.Author), FormFieldType.Relation, Zones.Publish,
            ReadOnly: true, Editor: PostFormEditors.Author, ModelName: "user"),
        new(Categories, nameof(AppRes.Categories), FormFieldType.Relation, Zones.Extra,
            Feature: PostTypeConstants.Features.Category, Multiple: true,
            Editor: PostFormEditors.Categories, ModelName: "postcategory"),
        new(Tags, nameof(AppRes.Tags), FormFieldType.String, Zones.Extra,
            Feature: PostTypeConstants.Features.Tags, Multiple: true, Editor: PostFormEditors.Tags),
    ];

    public static SystemFieldSlot? Find(string? key)
        => string.IsNullOrEmpty(key) ? null : All.FirstOrDefault(s => s.Key == key);
}

/// <summary>
/// Ключи доменных редакторов формы поста: компоненты регистрирует админка
/// (<c>FormEditorLocator.Register</c>), сервер только объявляет ключ в дескрипторе.
/// </summary>
public static class PostFormEditors
{
    /// <summary>Заголовок с авто-подстановкой slug</summary>
    public const string Title = "post.input.title";

    /// <summary>Статус из списка статусов типа</summary>
    public const string Status = "post.select.status";

    /// <summary>Мультивыбор категорий типа</summary>
    public const string Categories = "post.picker.categories";

    /// <summary>Ввод тегов</summary>
    public const string Tags = "post.input.tags";

    /// <summary>Автор: только чтение (пикера пользователя пока нет)</summary>
    public const string Author = "post.display.author";

    public static readonly IReadOnlyList<string> All = [Title, Status, Categories, Tags, Author];
}
