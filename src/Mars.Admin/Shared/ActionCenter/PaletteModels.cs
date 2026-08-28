using Mars.Cms.Contracts.Search;
using Mars.Contracts.XActions;

namespace Mars.Admin.Shared.ActionCenter;

public enum PaletteItemKind
{
    Pinned,
    Command,
    SearchResult,
    RecentPage,
    Page,
    Option,
}

/// <summary>
/// Закреплённые команды палитры (встроенные, управляют её режимами).
/// </summary>
public enum PinnedCommand
{
    GoToPage,
    RunCommand,
    Search,
    OpenAiChat,
    OpenNodesEditor,
}

/// <summary>
/// Единый пункт списка палитры: закреплённая команда, команда, результат поиска,
/// недавняя или целевая страница. Рендерится в одну строку (название + хвост).
/// </summary>
public class PaletteItem
{
    public required string Id { get; init; }
    public required string Title { get; init; }

    /// <summary>Хвост строки: показывается после названия с отступом.</summary>
    public string? Description { get; init; }

    public required PaletteItemKind Kind { get; init; }

    public string? Url { get; init; }
    public string? IconClass { get; init; }
    public XActionCommand? Command { get; init; }
    public SearchFoundElementResponse? SearchResult { get; init; }
    public PinnedCommand? Pinned { get; init; }

    /// <summary>Позиция в плоском списке выбираемых пунктов (для клавиатуры).</summary>
    public int FlatIndex { get; set; }
}

/// <summary>
/// Секция списка палитры: элементы, опционально предваряемые горизонтальным разделителем.
/// Заголовки секций не используются — разделение только разделителями.
/// </summary>
public class PaletteSection
{
    public bool DividerBefore { get; init; }
    public List<PaletteItem> Items { get; init; } = [];
}
