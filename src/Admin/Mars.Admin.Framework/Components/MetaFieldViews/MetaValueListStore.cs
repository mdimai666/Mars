using System.Collections;
using Mars.Forms.Contracts;
using Mars.Forms.Front;

namespace Mars.Admin.Framework.Components.MetaFieldViews;

/// <summary>
/// Значения формы владельца в виде EAV-строк мета-значений (пользователи, категории постов):
/// значение поля — строка с <c>Index</c>, множественное — список строк по порядку индексов.
/// Типизированной модели у таких владельцев нет, поэтому строки и есть значения формы.
/// </summary>
public sealed class MetaValueListStore(List<MetaValueEditModel> rows) : IFormValueStore
{
    public IReadOnlyCollection<FormError> Errors { get; set; } = [];

    public event Action? Changed;

    public object? GetValue(FormFieldDescriptor field)
        => IsList(field) ? GetList(field) : Row(field);

    public IReadOnlyList<object?> GetList(FormFieldDescriptor field)
        => rows.Where(r => r.MetaField.Key == field.Key)
               .OrderBy(r => r.Index)
               .Cast<object?>()
               .ToList();

    public void SetValue(FormFieldDescriptor field, object? value)
    {
        // множественное поле: редактор отдаёт значение целиком списком строк
        if (IsList(field))
        {
            SetList(field, value is IEnumerable items ? items.Cast<object?>() : []);
            return;
        }

        if (value is not MetaValueEditModel row) return;
        if (rows.Any(r => r.MetaField.Key == field.Key && r.Index == row.Index)) return;

        rows.Add(row);
        Changed?.Invoke();
    }

    public void SetList(FormFieldDescriptor field, IEnumerable<object?> values)
    {
        var items = values.OfType<MetaValueEditModel>().ToList();
        for (var i = 0; i < items.Count; i++) items[i].Index = i;

        rows.RemoveAll(r => r.MetaField.Key == field.Key);
        rows.AddRange(items);
        Changed?.Invoke();
    }

    /// <summary>Носитель значения — сам список строк владельца: доменные редакторы правят его напрямую</summary>
    public object? NativeValue(FormFieldDescriptor field) => rows;

    static bool IsList(FormFieldDescriptor field)
        => field.Multiple || field.Type == FormFieldType.SelectMany;

    MetaValueEditModel? Row(FormFieldDescriptor field)
        => rows.FirstOrDefault(r => r.MetaField.Key == field.Key && r.Index == 0);
}
