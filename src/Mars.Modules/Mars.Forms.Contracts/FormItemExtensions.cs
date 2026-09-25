namespace Mars.Forms.Contracts;

/// <summary>Преобразования элементов раскладки формы</summary>
public static class FormItemExtensions
{
    /// <summary>
    /// Копия элемента без дескриптора — то, что сохраняется в раскладке.
    /// Дескрипторы не хранятся: их отдаёт провайдер, иначе они устаревают.
    /// </summary>
    public static FormItem ToLayout(this FormItem item) => item with { Field = null };

    public static FormLayoutSettings ToLayout(this IEnumerable<FormItem> items)
        => new() { Items = items.Select(i => i.ToLayout()).ToList() };

    /// <summary>Поля в порядке раскладки (структурные и неполевые узлы пропускаются)</summary>
    public static IEnumerable<FormItem> FlattenFields(this IEnumerable<FormItem> items)
        => items.Where(item => item.Field is not null);

    /// <summary>Поле по ключу; null — поля с таким ключом в форме нет</summary>
    public static FormItem? Field(this FormDefinition definition, string key)
        => definition.Fields().FirstOrDefault(item => item.Key == key);
}
