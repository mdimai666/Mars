using System.ComponentModel;
using System.Text;
using Mars.SiteEngine.Abstractions.Services;
using Mars.SiteEngine.Contracts.WebSite.Dto;

namespace Mars.AiChat.Host.Tools;

/// <summary>
/// Инструменты агента для работы с файлами фронта (Handlebars-шаблоны сайта).
/// Работают через общий <see cref="IFrontFilesService"/> (защита путей наследуется).
/// Экземпляр создаётся на каждый запуск агента, если открыт редактор фронта —
/// slug берётся из PageContext (URL страницы /front/editor/{slug}).
/// </summary>
public class MarsFrontFilesTools
{
    /// <summary>Максимум символов содержимого файла, отдаваемый модели за одно чтение.</summary>
    const int MaxReadChars = 100_000;

    /// <summary>Максимум строк в дереве файлов, отдаваемый модели.</summary>
    const int MaxTreeLines = 500;

    private readonly IFrontFilesService _files;
    private readonly string _slug;

    public MarsFrontFilesTools(IFrontFilesService files, string slug)
    {
        _files = files;
        _slug = slug;
    }

    /// <summary>
    /// Slug фронта из PageContext страницы редактора (/front/editor/{slug}).
    /// null — открыта другая страница.
    /// </summary>
    public static string? TryParseSlugFromPageContext(string? pageContext)
    {
        if (string.IsNullOrWhiteSpace(pageContext)) return null;

        var segments = pageContext.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i + 2 < segments.Length; i++)
        {
            if (segments[i].Equals("front", StringComparison.OrdinalIgnoreCase)
                && segments[i + 1].Equals("editor", StringComparison.OrdinalIgnoreCase))
            {
                var slug = Uri.UnescapeDataString(segments[i + 2]);
                return string.IsNullOrWhiteSpace(slug) ? null : slug;
            }
        }

        return null;
    }

    [Description("Показать дерево файлов фронта: папки и файлы с путями относительно корня фронта. " +
                 "Вызывай первым, чтобы понять структуру шаблонов.")]
    public string ListFrontFiles()
    {
        try
        {
            var tree = _files.GetTree(_slug);

            var sb = new StringBuilder();
            var lines = 0;
            AppendNode(tree.Children, "", sb, ref lines);

            return sb.Length > 0 ? sb.ToString() : "Фронт пуст (файлов нет).";
        }
        catch (Exception ex)
        {
            return Error(ex);
        }
    }

    static void AppendNode(List<FFrontTreeNodeResponse> nodes, string indent, StringBuilder sb, ref int lines)
    {
        foreach (var node in nodes)
        {
            if (lines >= MaxTreeLines) return;

            sb.Append(indent).Append(node.IsDirectory ? node.Name + "/" : node.Name).Append('\n');
            lines++;

            if (node.IsDirectory)
                AppendNode(node.Children, indent + "  ", sb, ref lines);
        }
    }

    [Description("Прочитать файл фронта по относительному пути (например \"pages/index_page.hbs\" или \"wwwroot/css/app.css\"). " +
                 "Обязательно прочитай файл перед правкой.")]
    public string ReadFrontFile(
        [Description("Путь к файлу относительно корня фронта, например \"_root.hbs\" или \"pages/posts_page.hbs\"")] string relPath)
    {
        try
        {
            var file = _files.ReadFile(_slug, relPath);

            if (file.Content.Length > MaxReadChars)
            {
                return file.Content[..MaxReadChars]
                    + $"\n\n[Файл большой: показано первые {MaxReadChars} символов из {file.Content.Length}.]";
            }

            return file.Content;
        }
        catch (Exception ex)
        {
            return Error(ex);
        }
    }

    [Description("Записать файл фронта: создать новый или полностью заменить существующий (папки создаются автоматически). " +
                 "Файл сохраняется сразу — предпросмотр у пользователя обновится автоматически, отдельное сохранение не нужно. " +
                 "Передавай НОВОЕ ПОЛНОЕ содержимое файла; для точечной правки сначала прочитай файл.")]
    public string WriteFrontFile(
        [Description("Путь к файлу относительно корня фронта, например \"pages/about.hbs\"")] string relPath,
        [Description("Новое полное содержимое файла")] string content)
    {
        try
        {
            _files.SaveFile(_slug, relPath, content);
            return $"Файл '{relPath}' сохранён. Изменения применятся автоматически (предпросмотр обновится).";
        }
        catch (Exception ex)
        {
            return Error(ex);
        }
    }

    [Description("Создать пустой файл или папку во фронте. Для создания файла сразу с содержимым используй WriteFrontFile.")]
    public string CreateFrontFile(
        [Description("Путь относительно корня фронта, например \"pages/new_page.hbs\" или \"blocks\"")] string relPath,
        [Description("true — создать папку, false (по умолчанию) — файл")] bool isFolder = false)
    {
        try
        {
            if (isFolder) _files.CreateFolder(_slug, relPath);
            else _files.CreateFile(_slug, relPath);

            return $"Создано: '{relPath}'{(isFolder ? " (папка)" : "")}.";
        }
        catch (Exception ex)
        {
            return Error(ex);
        }
    }

    [Description("Переименовать или переместить файл/папку фронта. Атомарная операция: старый путь исчезает, " +
                 "содержимое сохраняется. Для переименования всегда используй её, а не создание нового файла рядом.")]
    public string RenameFrontFile(
        [Description("Текущий путь относительно корня фронта, например \"pages/old_page.hbs\"")] string relPath,
        [Description("Новый путь относительно корня фронта, например \"pages/new_page.hbs\"")] string newRelPath)
    {
        try
        {
            _files.Rename(_slug, relPath, newRelPath);
            return $"Переименовано: '{relPath}' → '{newRelPath}'.";
        }
        catch (Exception ex)
        {
            return Error(ex);
        }
    }

    [Description("Удалить файл или папку фронта. Опасное действие — сначала подтверди у пользователя через AskUser.")]
    public string DeleteFrontFile(
        [Description("Путь относительно корня фронта, например \"pages/old_page.hbs\"")] string relPath)
    {
        try
        {
            _files.Delete(_slug, relPath);
            return $"Удалено: '{relPath}'.";
        }
        catch (Exception ex)
        {
            return Error(ex);
        }
    }

    static string Error(Exception ex)
        => "Ошибка: " + ex.GetBaseException().Message;
}
