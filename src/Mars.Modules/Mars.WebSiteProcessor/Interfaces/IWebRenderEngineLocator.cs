using Mars.Host.Shared.Models;
using Mars.Host.Shared.TemplateEngine;
using Microsoft.AspNetCore.Http;

namespace Mars.WebSiteProcessor.Interfaces;

/// <summary>
/// Синглтон. Резолвит фронт и его движок рендера по URL запроса из актуальных настроек (FrontsOption).
/// Кэш движков пересобирается при изменении списка фронтов — без рестарта приложения.
/// </summary>
public interface IWebRenderEngineLocator
{
    /// <summary>
    /// Актуальный MarsAppFront для URL (только включенные фронты). null — фронт не найден.
    /// </summary>
    MarsAppFront? GetAppFrontForUrl(string url);

    /// <summary>
    /// MarsAppFront по slug фронта (включая выключенные). null — фронт не найден.
    /// </summary>
    MarsAppFront? GetAppFrontBySlug(string slug);

    /// <summary>
    /// MarsAppFront по slug только если движок уже создан (ленивый кэш).
    /// В отличие от <see cref="GetAppFrontBySlug"/> не создаёт движок как побочный эффект.
    /// </summary>
    MarsAppFront? TryGetAppFrontBySlug(string slug);

    /// <summary>
    /// Отдаёт статику из wwwroot фронта, если файл существует. true — ответ уже записан.
    /// </summary>
    Task<bool> TryServeStaticFileAsync(HttpContext context, MarsAppFront appFront);

    /// <summary>
    /// Доступные движки рендера (встроенные + плагинные)
    /// </summary>
    IReadOnlyCollection<EngineMetadata> GetAvailableEngines();
}
