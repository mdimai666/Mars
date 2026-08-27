using System.Reflection;

namespace AppFront.Shared.Models;

/// <summary>
/// Тип компонента в Blazor-сборке.
/// </summary>
public enum EComponentType
{
    ComponentBase,
    Page,
    Layout,
    Other
}

/// <summary>
/// Информация о Blazor-странице/компоненте, извлечённая из сборки через рефлексию
/// (маршруты, роли, layout, путь к исходному файлу).
/// </summary>
public class BlazorPageInfo
{
    /// <summary>Имя класса.</summary>
    public required string Name { get; init; }

    public required Type PageType { get; init; }

    public required Assembly Assembly { get; init; }

    public required EComponentType Kind { get; init; }

    /// <summary>Маршруты из RouteAttribute (@page), без префикса маунта приложения.</summary>
    public required IReadOnlyList<string> Routes { get; init; }

    /// <summary>Роли из AuthorizeAttribute.Roles (разбитые по запятым).</summary>
    public required IReadOnlyList<string> Roles { get; init; }

    /// <summary>Страница помечена [Authorize] (даже без указания ролей — нужен вход).</summary>
    public bool RequiresAuthorization { get; init; }

    /// <summary>Страница помечена [AllowAnonymous].</summary>
    public bool AllowsAnonymous { get; init; }

    /// <summary>Layout из LayoutAttribute (@layout в .razor или унаследованный из _Imports.razor).</summary>
    public Type? LayoutType { get; init; }

    /// <summary>Имя из DisplayAttribute либо хуманизированное имя класса.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Описание (задел под будущие метаданные).</summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// Путь относительно корня исходников (namespace → папки), например
    /// <c>AppAdmin/Pages/Index.razor</c>. Заполняется всегда
    /// (работает и в WASM, где Assembly.Location пуст).
    /// </summary>
    public string? SourceRelativePath { get; init; }

    /// <summary>
    /// Абсолютный путь к исходному файлу. Best-effort: только вне браузера и в Debug-сборке,
    /// если файл удалось найти на диске; иначе null.
    /// </summary>
    public string? SourceFilePath { get; init; }
}
