using System.Reflection;
using Mars.Admin.Framework.Models;

namespace Mars.Admin.Framework.Interfaces;

/// <summary>
/// Извлекает Blazor-страницы (компоненты с [Route]) из сборок:
/// маршруты, роли, layout, отображаемое имя, путь к исходному файлу.
/// Сценарии: «открыть исходный код», поиск страниц,
/// список страниц для агента и для e2e-скриншотов.
/// </summary>
public interface IBlazorPagesService
{
    /// <summary>Все компоненты сборки (страницы, layout-ы и обычные компоненты).</summary>
    IReadOnlyList<BlazorPageInfo> GetPages(Assembly assembly);

    /// <summary>Все компоненты из нескольких сборок (например, приложение + плагины).</summary>
    IReadOnlyList<BlazorPageInfo> GetPages(IEnumerable<Assembly> assemblies);

    /// <summary>Только страницы (Kind == Page, т.е. с хотя бы одним маршрутом).</summary>
    IReadOnlyList<BlazorPageInfo> GetRoutedPages(IEnumerable<Assembly> assemblies);

    /// <summary>
    /// Страницы, у которых есть хотя бы один маршрут без параметров ({...}) —
    /// по ним можно ходить напрямую: меню, скриншоты, подсказки агенту.
    /// </summary>
    IReadOnlyList<BlazorPageInfo> GetStaticRoutedPages(IEnumerable<Assembly> assemblies);

    /// <summary>Поиск по имени класса, DisplayName или маршруту (без учёта регистра).</summary>
    IReadOnlyList<BlazorPageInfo> Search(IEnumerable<Assembly> assemblies, string query);

    /// <summary>
    /// Ищет страницу по URL (относительному к базе приложения, без префикса маунта,
    /// либо абсолютному — от него берётся только путь).
    /// GUID-ы в URL подставляются под шаблоны маршрутов вида <c>/User/{ID:guid}</c>.
    /// </summary>
    BlazorPageInfo? FindPageByUrl(IEnumerable<Assembly> assemblies, string url);

    /// <summary>
    /// Путь к исходному файлу класса страницы. Абсолютный, если удалось найти на диске
    /// (Debug вне браузера), иначе относительный (namespace → папки). null, если ничего не найдено.
    /// </summary>
    string? ResolveSourceFilePath(Type pageType);
}
