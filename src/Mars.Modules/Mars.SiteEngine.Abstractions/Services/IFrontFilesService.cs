using Mars.SiteEngine.Contracts.WebSite.Dto;
using Mars.SiteEngine.Contracts.Options;

namespace Mars.SiteEngine.Abstractions.Services;

/// <summary>
/// Файловые операции над папкой фронта. Используется REST-контроллером (админка)
/// и ИИ-инструментами. Все пути — только относительные, с проверкой выхода за корень фронта.
/// </summary>
public interface IFrontFilesService
{
    FrontItem GetFront(string slug);

    /// <summary>Физический корень папки фронта; бросается, если папки нет.</summary>
    string GetFrontRoot(string slug);

    /// <summary>
    /// Резолвит относительный путь внутри корня фронта; бросается при выходе за его пределы.
    /// </summary>
    string ResolveSafePath(string slug, string relPath);

    FFrontTreeNodeResponse GetTree(string slug);

    FFrontFileContentResponse ReadFile(string slug, string relPath);

    /// <summary>Создаёт или полностью заменяет файл (папки создаются автоматически).</summary>
    void SaveFile(string slug, string relPath, string content);

    void CreateFile(string slug, string relPath);

    void CreateFolder(string slug, string relPath);

    void Rename(string slug, string relPath, string newRelPath);

    void Delete(string slug, string relPath);
}
