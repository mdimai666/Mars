using Mars.Cms.Contracts.MetaFields;
using Mars.Contracts.Resources;

namespace Mars.Cms.Contracts.PostTypes;

/// <summary>
/// Колонки грида постов типа: состав доступных колонок (базовые из <see cref="SystemFieldsCatalog"/>
/// плюс мета-поля типа) и слияние с сохранённым порядком. Общий источник для диалога настройки
/// колонок и для рендера грида.
/// </summary>
public static class PostTypeGridColumns
{
    /// <summary>
    /// Базовые колонки грида в порядке по умолчанию. Порядок и гейты свои, а не из каталога:
    /// форма прячет created_at без ModifyCreatedDate, а грид показывает его всегда.
    /// </summary>
    static readonly (string Key, string? Feature)[] BaseOrder =
    [
        (SystemFieldsCatalog.Title, null),
        (SystemFieldsCatalog.Categories, PostTypeConstants.Features.Category),
        (SystemFieldsCatalog.Status, PostTypeConstants.Features.Status),
        (SystemFieldsCatalog.Author, null),
        (SystemFieldsCatalog.CreatedAt, null),
    ];

    /// <summary>Ключи базовых колонок грида в порядке по умолчанию</summary>
    public static IReadOnlyList<string> BaseKeys { get; } = BaseOrder.Select(b => b.Key).ToList();

    /// <summary>Доступные колонки: базовые (с учётом фич типа) + мета-поля типа</summary>
    public static IReadOnlyList<PostTypeGridColumnInfo> Available(
        IReadOnlyCollection<string>? enabledFeatures,
        IEnumerable<MetaFieldDetailResponse>? metaFields)
    {
        var columns = new List<PostTypeGridColumnInfo>();

        foreach (var (key, feature) in BaseOrder)
        {
            if (feature is not null && !(enabledFeatures?.Contains(feature) ?? false)) continue;
            columns.Add(new PostTypeGridColumnInfo(key, SystemFieldsCatalog.Find(key)!.TitleKey));
        }

        foreach (var field in metaFields ?? [])
        {
            // плоскому гриду не подходят многовариантные и вычислимые поля
            if (field.Type is MetaFieldType.Query or MetaFieldType.SelectMany) continue;
            columns.Add(new PostTypeGridColumnInfo(field.Key, MetaTitle: field.Title));
        }

        return columns;
    }

    /// <summary>
    /// Слияние сохранённого порядка с доступными колонками: настроенные идут в заданном порядке
    /// со своей видимостью, неизвестные ключи отбрасываются, ненастроенные добавляются в конец видимыми.
    /// </summary>
    public static IReadOnlyList<PostTypeGridColumnInfo> Merge(
        IReadOnlyCollection<PostTypeGridColumn>? configured,
        IEnumerable<PostTypeGridColumnInfo> available)
    {
        var rest = available.ToList();
        var merged = new List<PostTypeGridColumnInfo>(rest.Count);

        foreach (var column in configured ?? [])
        {
            var index = rest.FindIndex(c => c.Key == column.Key);
            if (index < 0) continue;

            merged.Add(rest[index] with { Visible = column.Visible });
            rest.RemoveAt(index);
        }

        merged.AddRange(rest);
        return merged;
    }
}

/// <summary>
/// Колонка грида постов. Для базовой колонки задан <see cref="TitleKey"/> (ключ ресурса
/// <see cref="AppRes"/> из каталога системных слотов), для мета-колонки — <see cref="MetaTitle"/>.
/// </summary>
public sealed record PostTypeGridColumnInfo(string Key, string? TitleKey = null, string? MetaTitle = null)
{
    /// <summary>Базовая колонка из каталога системных слотов (не мета-поле)</summary>
    public bool IsSystem => TitleKey is not null;

    /// <summary>false — колонка скрыта настройкой презентации типа</summary>
    public bool Visible { get; init; } = true;

    /// <summary>Отображаемый заголовок колонки</summary>
    public string Title => MetaTitle ?? (TitleKey switch
    {
        nameof(AppRes.Title) => AppRes.Title,
        nameof(AppRes.Categories) => AppRes.Categories,
        nameof(AppRes.Status) => AppRes.Status,
        nameof(AppRes.Author) => AppRes.Author,
        nameof(AppRes.CreatedAt) => AppRes.CreatedAt,
        _ => Key,
    });
}
