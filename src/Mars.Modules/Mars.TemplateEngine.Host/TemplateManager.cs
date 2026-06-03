using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using Mars.Host.Shared.TemplateEngine;

namespace Mars.TemplateEngine.Host;

public class TemplateManager : ITemplateManager
{
    private readonly Dictionary<string, ITemplateEngine> _engines;
    private readonly Dictionary<string, EngineMetadata> _enginesMetadata;

    public TemplateManager(IEnumerable<ITemplateEngine> registeredEngines)
    {
        _engines = new Dictionary<string, ITemplateEngine>(StringComparer.OrdinalIgnoreCase);
        _enginesMetadata = [];

        foreach (var engine in registeredEngines)
        {
            _engines[engine.Id] = engine;

            // Извлекаем метаданные из атрибута [Display]
            var displayAttribute = engine.GetType().GetCustomAttribute<DisplayAttribute>();

            string name = displayAttribute?.GetName() ?? engine.GetType().Name;
            string description = displayAttribute?.GetDescription() ?? string.Empty;

            _enginesMetadata.Add(engine.Id, new EngineMetadata(engine.Id, name, description));
        }
    }

    public IEnumerable<EngineMetadata> GetAvailableEngines()
    {
        return _enginesMetadata.Values;
    }

    public EngineMetadata? GetEngineById(string engineId)
    {
        return _enginesMetadata.GetValueOrDefault(engineId);
    }

    public RenderResult RenderCached(string engineId, string templateId, string template, object context)
    {
        if (!_engines.TryGetValue(engineId, out var engine))
            throw new NotSupportedException($"Шаблонизатор с идентификатором '{engineId}' не найден в системе.");

        long bytesBefore = 0;

        // Аллокация измеряется только в режиме Debug
#if DEBUG
        GC.GetAllocatedBytesForCurrentThread(); // Прогрев счетчика
        bytesBefore = GC.GetAllocatedBytesForCurrentThread();
#endif

        var stopwatch = Stopwatch.StartNew();

        string content = engine.RenderCached(templateId, template, context);

        stopwatch.Stop();

        long allocatedBytes = 0;
#if DEBUG
        long bytesAfter = GC.GetAllocatedBytesForCurrentThread();
        allocatedBytes = bytesAfter - bytesBefore;
        if (allocatedBytes < 0) allocatedBytes = 0;
#endif

        return new RenderResult(content, stopwatch.Elapsed, allocatedBytes);
    }

    /// <summary>
    /// Очищает кэш для конкретного движка по его строковому ID
    /// </summary>
    public void ClearCacheFor(string engineId)
    {
        if (_engines.TryGetValue(engineId, out var engine))
        {
            engine.ClearCache();
        }
    }

    public void ClearAllCache()
    {
        foreach (var engine in _engines.Values)
        {
            engine.ClearCache();
        }
    }
}
