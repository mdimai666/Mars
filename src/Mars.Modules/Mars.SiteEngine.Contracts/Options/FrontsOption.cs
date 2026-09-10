namespace Mars.SiteEngine.Contracts.Options;

/// <summary>
/// Список фронтов сайта (файловые фронты в data/fronts или внешние папки).
/// Заменяет секцию "AppFront" из appsettings.
/// </summary>
public class FrontsOption
{
    /// <summary>
    /// Значение выбора фронта в визарде: существующая папка с шаблонами (путь + движок).
    /// </summary>
    public const string ExistingFrontChoice = "existing";

    public List<FrontItem> Fronts { get; set; } = [];

    /// <summary>
    /// Прогревать рендер после запуска приложения: первый фронт собирается заранее,
    /// чтобы первый запрос не платил за это. По умолчанию выключено.
    /// </summary>
    public bool WarmupRenderOnStartup { get; set; }
}

public class FrontItem
{
    public const string HandlebarsEngine = "handlebars";

    string _url = "";

    /// <summary>
    /// Имя папки фронта в data/fronts (или имя внешнего фронта)
    /// </summary>
    public string Slug { get; set; } = "";

    public string Title { get; set; } = "";

    /// <summary>
    /// Точка маунта: "" (корень), "/app2" и т.д.
    /// </summary>
    public string Url { get => _url; set => _url = value?.ToLowerInvariant().TrimEnd('/') ?? ""; }

    /// <summary>
    /// Пусто = папка по умолчанию data/fronts/&lt;Slug&gt;, иначе внешняя папка (абсолютный путь)
    /// </summary>
    public string Path { get; set; } = "";

    /// <summary>
    /// Id движка рендера (реестр фабрик IWebRenderEngineFactory)
    /// </summary>
    public string EngineId { get; set; } = HandlebarsEngine;

    public bool Enabled { get; set; } = true;
}
