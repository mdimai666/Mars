namespace Mars.Admin.Framework.Components.MetaFieldViews;

/// <summary>
/// Доступ к значениям мета-полей по словарному ключу <c>(Key, Index)</c>:
/// транспорт страницы редактирования — плоский список значений,
/// одиночные значения читаются как <c>(key, 0)</c>.
/// </summary>
public static class MetaValueEditModelLookup
{
    public static Dictionary<(string Key, int Index), MetaValueEditModel> ToKeyIndexDictionary(this IEnumerable<MetaValueEditModel> values)
        => values.ToDictionary(v => (v.Key, v.Index));

    public static MetaValueEditModel? GetValue(this IReadOnlyDictionary<(string Key, int Index), MetaValueEditModel> values, string key, int index = 0)
        => values.TryGetValue((key, index), out var value) ? value : null;
}
