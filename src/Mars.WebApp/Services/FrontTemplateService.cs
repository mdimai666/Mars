namespace Mars.Services;

/// <summary>
/// Создание фронтов из стартовых шаблонов (Res/front_templates/&lt;name&gt; → data/fronts/&lt;slug&gt;)
/// </summary>
public class FrontTemplateService
{
    public const string DefaultTemplateName = "default";
    public const string LandingTemplateName = "landing";

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

    /// <summary>
    /// Стартовые шаблоны для новых фронтов (папки Res/front_templates).
    /// Специальные шаблоны (админ-фронта) не входят в список.
    /// </summary>
    public IReadOnlyCollection<string> GetStarterTemplates()
    {
        var root = Path.Combine(env.ContentRootPath, "Res", "front_templates");
        if (!Directory.Exists(root)) return [];

        var names = new List<string>();
        foreach (var dir in Directory.GetDirectories(root))
        {
            var name = Path.GetFileName(dir);
            if (string.IsNullOrEmpty(name)) continue;
            if (string.Equals(name, AdminTemplateName, StringComparison.OrdinalIgnoreCase)) continue;

            names.Add(name);
        }

        names.Sort(StringComparer.OrdinalIgnoreCase);
        return names;
    }

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

    /// <summary>
    /// Папка специального фронта админки: data/admin/front
    /// </summary>
    public string AdminFrontPath => Path.Combine(env.ContentRootPath, FrontManager.AdminFrontDirName);

    /// <summary>
    /// Имя стартового шаблона админ-фронта в Res/front_templates.
    /// </summary>
    public const string AdminTemplateName = "admin";

    /// <summary>
    /// Создаёт специальный фронт админки (data/admin/front) из шаблона Res/front_templates/admin,
    /// дозаполняя отсутствующие файлы (_root.hbs, admin_index.hbs и др.).
    /// Вызывается при старте; существующие файлы не затирает.
    /// </summary>
    public void EnsureAdminFront()
    {
        var templatePath = GetTemplatePath(AdminTemplateName);
        if (!Directory.Exists(templatePath))
            throw new DirectoryNotFoundException($"Шаблон админ-фронта не найден '{templatePath}'");

        CopyMissingFiles(templatePath, AdminFrontPath);
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

    /// <summary>
    /// Рекурсивно копирует только отсутствующие в назначении файлы (не затирая правки пользователя).
    /// </summary>
    static void CopyMissingFiles(string sourcePath, string destPath)
    {
        Directory.CreateDirectory(destPath);

        foreach (var file in Directory.GetFiles(sourcePath))
        {
            var dest = Path.Combine(destPath, Path.GetFileName(file));
            if (!File.Exists(dest))
                File.Copy(file, dest);
        }

        foreach (var dir in Directory.GetDirectories(sourcePath))
        {
            CopyMissingFiles(dir, Path.Combine(destPath, Path.GetFileName(dir)));
        }
    }
}
