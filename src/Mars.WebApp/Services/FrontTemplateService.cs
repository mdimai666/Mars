namespace Mars.Services;

/// <summary>
/// Создание фронтов из стартовых шаблонов (Res/front_templates/&lt;name&gt; → data/fronts/&lt;slug&gt;)
/// </summary>
public class FrontTemplateService
{
    public const string DefaultTemplateName = "default";

    readonly IWebHostEnvironment env;

    public FrontTemplateService(IWebHostEnvironment env)
    {
        this.env = env;
    }

    /// <summary>
    /// Базовая папка фронтов: data/fronts
    /// </summary>
    public string FrontsBasePath => Path.Combine(env.ContentRootPath, "data", FrontManager.FrontsDirName);

    public string GetTemplatePath(string name = DefaultTemplateName)
        => Path.Combine(env.ContentRootPath, "Res", "front_templates", name);

    public void CreateFrontFromTemplate(string slug, string templateName = DefaultTemplateName)
    {
        if (!FrontManager.IsValidSlug(slug))
            throw new ArgumentException($"Некорректный slug фронта '{slug}'", nameof(slug));

        var templatePath = GetTemplatePath(templateName);
        if (!Directory.Exists(templatePath))
            throw new DirectoryNotFoundException($"Шаблон фронта не найден '{templatePath}'");

        var destPath = Path.Combine(FrontsBasePath, slug);
        if (Directory.Exists(destPath))
            throw new InvalidOperationException($"Папка фронта уже существует '{destPath}'");

        CopyDirectory(templatePath, destPath);
    }

    static void CopyDirectory(string sourcePath, string destPath)
    {
        Directory.CreateDirectory(destPath);

        foreach (var file in Directory.GetFiles(sourcePath))
        {
            File.Copy(file, Path.Combine(destPath, Path.GetFileName(file)));
        }

        foreach (var dir in Directory.GetDirectories(sourcePath))
        {
            CopyDirectory(dir, Path.Combine(destPath, Path.GetFileName(dir)));
        }
    }
}
