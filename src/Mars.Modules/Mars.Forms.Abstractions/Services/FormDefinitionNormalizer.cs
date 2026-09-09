using Mars.Forms.Contracts;

namespace Mars.Forms.Abstractions.Services;

internal class FormDefinitionNormalizer : IFormDefinitionNormalizer
{
    public IReadOnlyCollection<FormItem> Normalize(IReadOnlyCollection<FormItem>? saved,
                                                   IReadOnlyCollection<FormItem> defaults,
                                                   FormNormalizeOptions? options = null)
    {
        var maxDepth = (options ?? new FormNormalizeOptions()).MaxSectionDepth;
        var savedItems = saved ?? [];

        var available = new Dictionary<string, FormItem>(StringComparer.Ordinal);
        foreach (var item in defaults.FlattenFields())
        {
            if (item.Field is not null)
                available.TryAdd(item.Key, item);
        }

        var zones = ZoneOrder(defaults, savedItems).ToList();
        var buckets = zones.ToDictionary(zone => zone, _ => new List<FormItem>(), StringComparer.Ordinal);
        var firstZone = zones.Count > 0 ? zones[0] : "";

        // зона поля по умолчанию — для сохранённых элементов без зоны
        var defaultZones = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var root in defaults)
        {
            var zone = ZoneOr(root, firstZone);
            var fields = root.IsSection ? root.Items.FlattenFields() : Enumerable.Repeat(root, 1);
            foreach (var field in fields)
                defaultZones.TryAdd(field.Key, zone);
        }

        var usedFields = new HashSet<string>(StringComparer.Ordinal);
        var usedSections = new HashSet<string>(StringComparer.Ordinal);

        // 1. сохранённая раскладка: её порядок, её зоны, её настройки
        foreach (var item in savedItems)
        {
            var zone = ZoneOf(item);
            if (zone.Length == 0)
                zone = item.IsSection || !defaultZones.TryGetValue(item.Key, out var defaultZone)
                    ? firstZone
                    : defaultZone;

            if (!buckets.TryGetValue(zone, out var bucket))
            {
                bucket = [];
                buckets[zone] = bucket;
                zones.Add(zone);
            }

            Emit(bucket, item, 0, zone);
        }

        // 2. недостающие поля провайдера — в конец своей зоны
        foreach (var zone in zones.ToList())
        {
            foreach (var item in defaults.Where(i => ZoneOr(i, firstZone) == zone))
                AppendDefault(buckets[zone], item, zone);
        }

        return zones.SelectMany(zone => buckets[zone]).ToList();

        // сохранённый элемент: настройки из него, дескриптор всегда свежий из defaults
        void Emit(List<FormItem> target, FormItem item, int depth, string zone)
        {
            if (item.IsSection)
            {
                if (item.Key.Length == 0)
                {
                    foreach (var child in item.Items) Emit(target, child, depth, zone);
                    return;
                }
                if (!usedSections.Add(item.Key)) return;

                if (depth >= maxDepth)
                {
                    // глубже предела секции не складываем — дети поднимаются на текущий уровень
                    foreach (var child in item.Items) Emit(target, child, depth, zone);
                    return;
                }

                var children = new List<FormItem>();
                foreach (var child in item.Items) Emit(children, child, depth + 1, zone);
                target.Add(item with { Field = null, Items = children });
                return;
            }

            if (!available.TryGetValue(item.Key, out var def)) return; // ключ провайдера больше не доступен
            if (!usedFields.Add(item.Key)) return;                     // дубль

            var descriptor = def.Field!;
            target.Add(new FormItem
            {
                Kind = FormItemKinds.Field,
                Key = def.Key,
                Zone = depth == 0 ? zone : null,
                Title = item.Title,
                Visible = item.Visible,
                Width = item.Width,
                Field = descriptor,
                Rules = descriptor.SettingsOnForm ? item.Rules : [],
                Editor = descriptor.SettingsOnForm ? item.Editor : null,
            });
        }

        void AppendDefault(List<FormItem> target, FormItem item, string zone)
        {
            if (item.IsSection)
            {
                if (item.Key.Length > 0 && !usedSections.Add(item.Key)) return;

                var children = new List<FormItem>();
                foreach (var child in item.Items) AppendDefault(children, child, zone);
                target.Add(item with { Zone = zone, Items = children });
                return;
            }

            if (!usedFields.Add(item.Key)) return;
            target.Add(item with { Zone = zone });
        }
    }

    static string ZoneOf(FormItem item) => item.Zone ?? "";

    static string ZoneOr(FormItem item, string fallback)
        => string.IsNullOrEmpty(item.Zone) ? fallback : item.Zone;

    static IEnumerable<string> ZoneOrder(IEnumerable<FormItem> defaults, IReadOnlyCollection<FormItem> saved)
    {
        var zones = new List<string>();

        foreach (var item in defaults)
        {
            var zone = ZoneOf(item);
            if (!zones.Contains(zone)) zones.Add(zone);
        }
        foreach (var item in saved)
        {
            var zone = ZoneOf(item);
            if (!zones.Contains(zone)) zones.Add(zone);
        }

        return zones;
    }
}
