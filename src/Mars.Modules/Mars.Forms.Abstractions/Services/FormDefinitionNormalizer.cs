using Mars.Forms.Contracts;

namespace Mars.Forms.Abstractions.Services;

/// <summary>
/// Приводит раскладку владельца к действующей: сохранённые узлы сохраняют порядок, тип, зону,
/// видимость, ширину и тексты; поле с исчезнувшим ключом провайдера отбрасывается; узел с
/// отсутствующим или недопустимым родителем переезжает в корень зоны, а если и там недопустим —
/// отбрасывается (его дети переезжают в корень следом). Порядок узлов в списке не важен:
/// родитель находится по ключу, циклы разрываются. Недостающие поля провайдера дописываются
/// в конец списка. Дескрипторы всегда свежие. Идемпотентен.
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
            if (string.IsNullOrEmpty(item.Key)) continue;                // безымянный узел
            if (accepted.ContainsKey(item.Key)) continue;                // ключ узла уже занят

            var resolved = Resolve(item);
            if (resolved is null) continue;

            accepted.Add(resolved.Key, resolved);
            result.Add(resolved);
        }

        // 2. узлы без допустимого места убираем; их дети осиротеют и переедут в корень зоны
        foreach (var key in result.Select(node => node.Key).ToList())
        {
            var (placed, _) = Placement(accepted[key], accepted);
            if (placed) continue;
            if (FormLayoutRules.CanContain(null, accepted[key].Kind)) continue;

            accepted.Remove(key);
            result.RemoveAll(node => node.Key == key);
        }

        // 3. места узлов: зона наследуется от корня цепочки, разорванная цепочка — корень зоны
        for (var i = 0; i < result.Count; i++)
        {
            var node = result[i];
            var (placed, zone) = Placement(node, accepted);
            var fallback = defaultZones.GetValueOrDefault(node.Key, firstZone);

            result[i] = node with
            {
                Parent = placed ? node.Parent : null,
                Zone = placed && !string.IsNullOrEmpty(zone) ? zone : ZoneOr(node, fallback),
            };
        }

        // 4. недостающие поля провайдера — в конец своей зоны
        foreach (var item in defaults)
        {
            if (item.Field is null) continue;
            if (accepted.ContainsKey(item.Key)) continue;

            result.Add(item with { Zone = ZoneOr(item, defaultZones.GetValueOrDefault(item.Key, firstZone)) });
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
    /// Поднимается по родителям узла: <c>Placed = false</c> — родителя нет, он не годится или
    /// цепочка зациклена; иначе <c>Zone</c> — зона корня цепочки (её и наследует узел).
    /// </summary>
    static (bool Placed, string? Zone) Placement(FormItem node, Dictionary<string, FormItem> accepted)
    {
        var chain = new HashSet<string>(StringComparer.Ordinal) { node.Key };
        var current = node;

        while (current.Parent is { } parentKey
               && accepted.TryGetValue(parentKey, out var parent)
               && chain.Add(parentKey)
               && FormLayoutRules.CanContain(parent.Kind, current.Kind))
            current = parent;

        return (current.Parent is null, current.Zone);
    }

    static string ZoneOr(FormItem item, string fallback)
        => string.IsNullOrEmpty(item.Zone) ? fallback : item.Zone;
}
