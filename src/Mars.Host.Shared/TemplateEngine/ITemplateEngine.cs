namespace Mars.Host.Shared.TemplateEngine;

public interface ITemplateEngine
{
    string Id { get; }
    void ClearCache();
    bool RemoveFromCache(string id);
    string Render(string template, object context);

    /// <summary>
    /// Рендерит шаблон с использованием кэша по уникальному ID.
    /// Если шаблона нет в кэше — он скомпилируется, сохранится и выполнится.
    /// </summary>
    string RenderCached(string id, string template, object context);
}
