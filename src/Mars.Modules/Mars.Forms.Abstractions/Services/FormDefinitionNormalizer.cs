using Mars.Forms.Contracts;

namespace Mars.Forms.Abstractions.Services;

/// <summary>
/// Приводит раскладку владельца к действующей: сохранённые узлы сохраняют порядок, тип, зону,
/// видимость, ширину и тексты; поле с исчезнувшим ключом провайдера отбрасывается; узел с
/// отсутствующим или недопустимым родителем переезжает в корень зоны, а если и там недопустим —
/// отбрасывается (его дети переезжают в корень следом). Элементы живут только в колонках:
/// свободные (легаси-плоская раскладка) оборачиваются в один ряд зоны, ширина переезжает
/// на колонку. Порядок узлов в списке не важен: родитель находится по ключу, циклы разрываются.
/// Недостающие поля провайдера дописываются в конец. Дескрипторы всегда свежие. Идемпотентен.
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

        // 2. узлы без допустимого места убираем; их дети осиротеют и переедут в корень зоны.
        //    Элемент не отбрасываем никогда — он переезжает в корень и будет обёрнут в колонку.
        foreach (var key in result.Select(node => node.Key).ToList())
        {
            var node = accepted[key];
            if (FormLayoutRules.IsElement(node.Kind)) continue;

            var (placed, _) = Placement(node, accepted);
            var asRoot = node.Parent is null || !placed;

            if (asRoot ? FormLayoutRules.CanContain(null, node.Kind) : placed) continue;

            accepted.Remove(key);
            result.RemoveAll(item => item.Key == key);
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

        // 4. недостающие поля провайдера — в конец, как и свободные элементы: обернёт шаг 5
        foreach (var item in defaults)
        {
            if (item.Field is null) continue;
            if (accepted.ContainsKey(item.Key)) continue;

            result.Add(item with { Zone = ZoneOr(item, defaultZones.GetValueOrDefault(item.Key, firstZone)) });
        }

        // 5. элементы — только в колонках
        return WrapElements(result);

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

    /// <summary>
    /// Оборачивает свободные элементы в колонки: в зоне и контейнере — одним рядом, а в ряду —
    /// прямо колонками. Раскладка по умолчанию выходит «одна строка и одна колонка на зону»:
    /// соседи без своей ширины живут в общей колонке, а элемент с шириной получает свою, поэтому
    /// легаси-плоская раскладка, где ширину задавал элемент, сохраняет свой вид.
    /// </summary>
    static List<FormItem> WrapElements(List<FormItem> items)
    {
        var kinds = items.ToDictionary(item => item.Key, item => item.Kind, StringComparer.Ordinal);
        var children = new Dictionary<(string Parent, string Zone), List<FormItem>>();
        foreach (var item in items)
        {
            // зона в ключе: корневые узлы разных зон неродственны, хотя родителя у них и нет
            var key = (item.Parent ?? "", item.Zone ?? "");
            if (!children.TryGetValue(key, out var list)) children[key] = list = [];
            list.Add(item);
        }

        var insert = new Dictionary<string, List<FormItem>>(StringComparer.Ordinal); // первый элемент прогона → новые узлы
        var reparent = new Dictionary<string, string>(StringComparer.Ordinal);       // элемент → его новая колонка

        foreach (var ((parentKey, zone), group) in children)
        {
            var parent = parentKey.Length == 0 ? null : parentKey;
            var parentKind = parent is null ? (FormItemKind?)null : kinds[parent];
            if (parentKind == FormItemKind.Column) continue;      // элемент в колонке уже на месте

            var inRow = parentKind == FormItemKind.Row;
            var columns = new List<FormItem>();
            FormItem? row = null;                                 // ряд, который получат новые колонки
            FormItem? shared = null;                              // открытая общая колонка соседей без ширины
            FormItem? anchor = null;                              // первый свободный элемент группы

            foreach (var child in group)
            {
                if (!FormLayoutRules.IsElement(child.Kind)) { shared = null; continue; }

                anchor ??= child;

                if (child.Width is null && shared is not null)    // сосед без ширины встаёт в общую колонку
                {
                    reparent[child.Key] = shared.Key;
                    continue;
                }

                if (!inRow)
                    row ??= new FormItem
                    {
                        Key = FormItem.NewKey(FormItemKind.Row),
                        Kind = FormItemKind.Row,
                        Parent = parent,
                        Zone = zone,
                    };

                var column = new FormItem
                {
                    Key = FormItem.NewKey(FormItemKind.Column),
                    Kind = FormItemKind.Column,
                    Parent = row?.Key ?? parent,
                    Zone = zone,
                    Width = child.Width,
                };

                columns.Add(column);
                reparent[child.Key] = column.Key;
                shared = child.Width is null ? column : null;      // своя ширина общую колонку не продолжает
            }

            if (anchor is null) continue;

            insert[anchor.Key] = row is null ? columns : [row, .. columns];
        }

        if (insert.Count == 0) return items;

        var result = new List<FormItem>(items.Count);
        foreach (var item in items)
        {
            if (insert.TryGetValue(item.Key, out var added)) result.AddRange(added);

            result.Add(reparent.TryGetValue(item.Key, out var column) ? item with { Parent = column } : item);
        }

        return result;
    }

    static string ZoneOr(FormItem item, string fallback)
        => string.IsNullOrEmpty(item.Zone) ? fallback : item.Zone;
}
