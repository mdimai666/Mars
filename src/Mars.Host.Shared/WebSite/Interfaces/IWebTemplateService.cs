using Mars.Host.Shared.WebSite.Models;

namespace Mars.Host.Shared.WebSite.Interfaces;

public interface IWebTemplateService
{
    public void ScanSite();

    public WebSiteTemplate Template { get; set; }

    /// <summary>
    /// Явное уведомление об изменении файла фронта (запись через FrontFilesService —
    /// REST админки, ИИ-инструменты). Выполняется сразу, в отличие от событий
    /// FileSystemWatcher: файловое событие может потеряться, а кеш рендера
    /// (скомпилированные шаблоны) живёт до 30 минут.
    /// </summary>
    public void NotifyFileChanged(string fullPath);
}