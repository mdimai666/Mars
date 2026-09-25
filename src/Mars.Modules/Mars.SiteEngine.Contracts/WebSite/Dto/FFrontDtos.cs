namespace Mars.SiteEngine.Contracts.WebSite.Dto;

public class FFrontEngineResponse
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = "";
}

/// <summary>
/// Стартовый шаблон фронта: имя папки в Res/front_templates и движок шаблона
/// (определяется по расширению файлов: *.sbn — scriban, иначе handlebars).
/// </summary>
public class FFrontTemplateResponse
{
    public required string Name { get; set; }
    public required string EngineId { get; set; }
}

public class FFrontTreeNodeResponse
{
    public required string Name { get; set; }

    /// <summary>
    /// Путь относительно корня фронта (через '/')
    /// </summary>
    public required string Path { get; set; }

    public bool IsDirectory { get; set; }

    public List<FFrontTreeNodeResponse> Children { get; set; } = [];
}

public class FFrontFileContentResponse
{
    public required string Path { get; set; }
    public string Content { get; set; } = "";
}

/// <summary>
/// Страница фронта: соответствие файла и его URL из атрибута @page
/// </summary>
public class FFrontPageResponse
{
    public required string FileRelPath { get; set; }
    public string Url { get; set; } = "";
}

public class FCreateFrontRequest
{
    public required string Slug { get; set; }
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public bool UseTemplate { get; set; } = true;

    /// <summary>
    /// Имя стартового шаблона из Res/front_templates. Пусто = шаблон по умолчанию.
    /// </summary>
    public string Template { get; set; } = "";

    /// <summary>
    /// Id рендер-движка (реестр IWebRenderEngineFactory). Пусто = движок по умолчанию.
    /// При создании из стартового шаблона игнорируется — движок диктуется шаблоном.
    /// </summary>
    public string EngineId { get; set; } = "";

    /// <summary>
    /// Только при UseTemplate=false: пустая папка data/fronts/&lt;slug&gt; создаётся,
    /// непустая — подключаемая существующая папка (абсолютный путь, должна существовать).
    /// </summary>
    public string Path { get; set; } = "";
}
