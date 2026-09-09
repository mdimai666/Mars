namespace Mars.Forms.Contracts;

/// <summary>Преобразования элементов дерева формы</summary>
public static class FormItemExtensions
{
    /// <summary>
    /// Копия элемента без дескриптора — то, что сохраняется в раскладке.
    /// Дескрипторы не хранятся: их отдаёт провайдер, иначе они устаревают.
    /// </summary>
    public static FormItem ToLayout(this FormItem item)
        => item with
        {
            Field = null,
            Items = item.Items.Select(i => i.ToLayout()).ToList(),
        };

    public static FormLayoutSettings ToLayout(this IEnumerable<FormItem> items)
        => new() { Items = items.Select(i => i.ToLayout()).ToList() };

    /// <summary>Все листы-поля дерева в порядке обхода</summary>
    public static IEnumerable<FormItem> FlattenFields(this IEnumerable<FormItem> items)
    {
        foreach (var item in items)
        {
            if (item.IsSection)
            {
                foreach (var child in item.Items.FlattenFields())
                    yield return child;
            }
            else
            {
                yield return item;
            }
        }
    }
}
