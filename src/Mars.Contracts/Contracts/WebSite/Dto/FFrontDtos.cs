namespace Mars.Shared.Contracts.WebSite.Dto;

public class FFrontEngineResponse
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = "";
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
}
