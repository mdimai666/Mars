using Mars.Forms.Contracts;

namespace Mars.Forms.Abstractions.Services;

/// <summary>
/// Приводит раскладку владельца к действующей: сохранённые узлы сохраняют порядок, тип, зону,
/// видимость, ширину и тексты; поле с исчезнувшим ключом провайдера отбрасывается; узел с
/// недопустимым или отсутствующим родителем переезжает в корень зоны (поддерево не теряется);
/// недостающие поля провайдера дописываются в конец своей зоны. Дескрипторы всегда свежие.
/// Идемпотентен: повторная нормализация не меняет результат.
/// </summary>
internal class FormDefinitionNormalizer : IFormDefinitionNormalizer
{
    public IReadOnlyCollection<FormItem> Normalize(IReadOnlyCollection<FormItem>? saved,
                                                   IReadOnlyCollection<FormItem> defaults)
    {
        var available = new Dictionary<string, FormItem>(StringComparer.Ordinal);
        var defaultZones = new Dictionary<string, string>(StringComparer.Ordinal);
        var firstZone = defaults.FirstOrDefault(item => !string.IsNullOrEmpty(item.Zone))?.Zone ?? "";

        foreach (var item in defaults)
        {
            if (item.Field is null) continue;

            available.TryAdd(item.Key, item);
            defaultZones.TryAdd(item.Key, ZoneOr(item, firstZone));
        }

        var accepted = new Dictionary<string, FormItem>(StringComparer.Ordinal);
        var result = new List<FormItem>();

        // 1. сохранённая раскладка: её порядок, её узлы и настройки
        foreach (var item in saved ?? [])
        {
            if (string.IsNullOrEmpty(item.Key)) continue;
            if (accepted.ContainsKey(item.Key)) continue;                // ключ узла уже занят

            var resolved = Resolve(item);
            if (resolved is null) continue;

            // поле без зоны встаёт в свою зону по умолчанию (status → publish), остальное — в первую
            var node = Attach(resolved, accepted, defaultZones.GetValueOrDefault(item.Key, firstZone));
            if (node is null) continue;

            accepted.Add(node.Key, node);
            result.Add(node);
        }

        // 2. недостающие поля провайдера — в конец своей зоны
        foreach (var item in defaults)
        {
            if (item.Field is null) continue;
            if (accepted.ContainsKey(item.Key)) continue;

            var node = item with { Zone = ZoneOr(item, defaultZones.GetValueOrDefault(item.Key, firstZone)) };
            accepted.Add(node.Key, node);
            result.Add(node);
        }

        return result;

        FormItem? Resolve(FormItem item)
        {
            if (item.Kind != FormItemKind.Field)
            {
                // ключ поля провайдера структурному узлу не отдаём: иначе поле исчезло бы из раскладки
                if (!FormLayoutRules.IsKnown(item.Kind)) return null;
                if (available.ContainsKey(item.Key)) return null;

                return item with { Field = null };
            }

            if (!available.TryGetValue(item.Key, out var def)) return null; // ключ провайдера больше не доступен

            return new FormItem
            {
                Key = def.Key,
                Parent = item.Parent,
                Zone = item.Zone,
                Title = item.Title,
                Visible = item.Visible,
                Width = item.Width,
                Field = def.Field,                                       // дескриптор всегда свежий
            };
        }
    }

    /// <summary>
    /// Родитель должен быть принят раньше и допускать такой тип ребёнка; иначе узел переезжает
    /// в корень зоны, а если и там недопустим — отбрасывается (его дети переедут в корень следом).
    /// </summary>
    static FormItem? Attach(FormItem node, Dictionary<string, FormItem> accepted, string fallbackZone)
    {
        if (node.Parent is not null
            && accepted.TryGetValue(node.Parent, out var parent)
            && FormLayoutRules.CanContain(parent.Kind, node.Kind))
            return node with { Zone = parent.Zone };

        return FormLayoutRules.CanContain(null, node.Kind)
            ? node with { Parent = null, Zone = ZoneOr(node, fallbackZone) }
            : null;
    }

    static string ZoneOr(FormItem item, string fallback)
        => string.IsNullOrEmpty(item.Zone) ? fallback : item.Zone;
}
