using System.Text.Json.Nodes;
using Mars.Forms.Contracts;

namespace Mars.Forms.Front;

/// <summary>
/// Клиентская обёртка над мешком значений формы: типизированный доступ по дескриптору через
/// <see cref="FormValueCodec"/>. Куда значение уйдёт на сервере, решает провайдер —
/// модель знает только ключ поля и его тип.
/// </summary>
public sealed class FormValuesModel(FormValues values) : IFormValueStore
{
    public FormValues Values { get; } = values;

    /// <summary>Ошибки от провайдера — для подсветки полей</summary>
    public IReadOnlyCollection<FormError> Errors { get; set; } = [];

    /// <summary>Сигнал изменения значения (рендерер перерисовывается сам, это для внешних подписчиков)</summary>
    public event Action? Changed;

    public JsonNode? Node(string key) => Values.Value(key);

    /// <summary>У мешка нет носителя значения: редакторы работают с CLR-значением</summary>
    public object? NativeValue(FormFieldDescriptor field) => null;

    public object? GetValue(FormFieldDescriptor field)
    {
        if (IsList(field)) return GetList(field);

        FormValueCodec.TryToClr(Values.Value(field.Key), field.Type, out var value, out _);
        return value;
    }

    public IReadOnlyList<object?> GetList(FormFieldDescriptor field)
        => FormValueCodec.TryToClrList(Values.Value(field.Key), field.ElementType, out var values, out _)
            ? values
            : [];

    public void SetValue(FormFieldDescriptor field, object? value)
    {
        Values.Values[field.Key] = IsList(field)
            ? FormValueCodec.FromClrList(AsList(value), field.ElementType)
            : FormValueCodec.FromClr(value, field.Type);
        Changed?.Invoke();
    }

    public void SetList(FormFieldDescriptor field, IEnumerable<object?> values)
    {
        Values.Values[field.Key] = FormValueCodec.FromClrList(values, field.ElementType);
        Changed?.Invoke();
    }

    static bool IsList(FormFieldDescriptor field)
        => field.Multiple || field.Type == FormFieldType.SelectMany;

    static IEnumerable<object?> AsList(object? value) => value switch
    {
        null => [],
        IEnumerable<object?> items => items,
        System.Collections.IEnumerable enumerable => enumerable.Cast<object?>(),
        _ => [value],
    };
}
