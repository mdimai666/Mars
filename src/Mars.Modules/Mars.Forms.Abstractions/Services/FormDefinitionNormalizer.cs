using Mars.Forms.Contracts;

namespace Mars.Forms.Abstractions.Services;

/// <summary>
/// Приводит раскладку владельца к действующему плоскому списку: порядок, зоны и настройки
/// сохраняются, неизвестные ключи отбрасываются, недостающие поля провайдера дописываются
/// в конец видимыми. Идемпотентен: повторная нормализация не меняет результат.
/// </summary>
internal class FormDefinitionNormalizer : IFormDefinitionNormalizer
{
    public IReadOnlyCollection<FormItem> Normalize(IReadOnlyCollection<FormItem>? saved,
                                                   IReadOnlyCollection<FormItem> defaults)
    {
        var available = new Dictionary<string, FormItem>(StringComparer.Ordinal);
        foreach (var item in defaults)
        {
            if (item.Field is not null)
                available.TryAdd(item.Key, item);
        }

        var firstZone = defaults.FirstOrDefault(item => !string.IsNullOrEmpty(item.Zone))?.Zone ?? "";

        var defaultZones = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in defaults)
            defaultZones.TryAdd(item.Key, ZoneOr(item, firstZone));

        var result = new List<FormItem>();
        var usedFields = new HashSet<string>(StringComparer.Ordinal);
        var usedMarkers = new HashSet<string>(StringComparer.Ordinal);

        // 1. сохранённая раскладка: её порядок, её зоны, её настройки
        foreach (var item in saved ?? [])
        {
            if (item.Kind == FormItemKind.Heading)
            {
                if (!usedMarkers.Add(item.Key)) continue;
                result.Add(item with { Zone = ZoneOr(item, firstZone), Field = null });
                continue;
            }

            if (!available.TryGetValue(item.Key, out var def)) continue; // ключ провайдера больше не доступен
            if (!usedFields.Add(item.Key)) continue;                     // дубль

            result.Add(new FormItem
            {
                Key = def.Key,
                Parent = item.Parent,
                Zone = ZoneOr(item, defaultZones.GetValueOrDefault(def.Key, firstZone)),
                Title = item.Title,
                Visible = item.Visible,
                Width = item.Width,
                Field = def.Field,                                       // дескриптор всегда свежий
            });
        }

        // 2. недостающие поля провайдера — в конец
        foreach (var item in defaults)
        {
            if (item.Field is null) continue;
            if (!usedFields.Add(item.Key)) continue;

            result.Add(item with { Zone = ZoneOr(item, firstZone) });
        }

        return result;
    }

    static string ZoneOr(FormItem item, string fallback)
        => string.IsNullOrEmpty(item.Zone) ? fallback : item.Zone;
}
