namespace Mars.Host.Shared.TemplateEngine;

public interface ITemplateManager
{
    void ClearAllCache();
    void ClearCacheFor(string engineId);
    IEnumerable<EngineMetadata> GetAvailableEngines();

    /// <summary>
    /// Рендерит кэшированный шаблон с замером времени, а также памяти (только в DEBUG)
    /// </summary>
    /// <param name="engineId">Уникальный ID движка, например "Core.Handlebars" или "plugin_name.handlebars"</param>
    RenderResult RenderCached(string engineId, string templateId, string template, object context);
}

public record RenderResult(
    string Content,
    TimeSpan Elapsed,
    long AllocatedBytes // Будет равен 0 в Release режиме
);

public record EngineMetadata(
    string Id,
    string Name,
    string Description
);
