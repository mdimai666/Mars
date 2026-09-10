using System.Collections;
using System.Globalization;
using Mars.Cms.Contracts.MetaFields;
using Mars.Forms.Contracts;
using Mars.Forms.Front;

namespace Mars.Admin.Framework.Components.MetaFieldViews;

/// <summary>
/// Значения формы владельца в EAV-строках мета-значений (посты, пользователи, категории):
/// наружу — канонические CLR-значения общего слоя (<see cref="FormValueCodec"/>) — строка, число,
/// дата, ключ варианта, Guid связи; внутрь — те же строки, что уходят в API (порядок = индекс).
/// Строки остаются носителем значения (<see cref="NativeValue"/>), поэтому доменные редакторы
/// (связи, файлы, списки объектов) правят их напрямую.
/// </summary>
public sealed class MetaValueStore(List<MetaValueEditModel> rows,
                                   IReadOnlyCollection<MetaFieldEditModel> fields) : IFormValueStore
{
    public IReadOnlyCollection<FormError> Errors { get; set; } = [];

    public event Action? Changed;

    public object? GetValue(FormFieldDescriptor field)
        => IsList(field) ? GetList(field) : Read(field, Row(field));

    public IReadOnlyList<object?> GetList(FormFieldDescriptor field)
    {
        // множественный выбор хранится одним значением поля: список ключей вариантов в строке
        if (field.Type == FormFieldType.SelectMany)
            return Keys(field, Row(field)?.VariantsIds ?? []).Cast<object?>().ToList();

        return RowsOf(field).Select(row => Read(field, row)).ToList();
    }

    public void SetValue(FormFieldDescriptor field, object? value)
    {
        if (IsList(field))
        {
            SetList(field, value is IEnumerable items ? items.Cast<object?>() : []);
            return;
        }

        Write(field, value, RowForWrite(field));
        Changed?.Invoke();
    }

    public void SetList(FormFieldDescriptor field, IEnumerable<object?> values)
    {
        var items = values.ToList();

        if (field.Type == FormFieldType.SelectMany)
        {
            RowForWrite(field).VariantsIds = Ids(field, items.OfType<string>()).ToArray();
            Changed?.Invoke();
            return;
        }

        // строки по индексу: существующие правим на месте (Id строки сохраняется), лишние убираем
        var existing = RowsOf(field).ToList();
        for (var i = 0; i < items.Count; i++)
            Write(field, items[i], i < existing.Count ? existing[i] : NewRow(field, i));

        foreach (var row in existing.Skip(items.Count)) rows.Remove(row);
        Changed?.Invoke();
    }

    public object? NativeValue(FormFieldDescriptor field) => rows;

    //=====================================

    object? Read(FormFieldDescriptor field, MetaValueEditModel? row) => field.Type switch
    {
        FormFieldType.String => row?.StringShort ?? "",
        FormFieldType.Text => row?.StringText ?? "",
        FormFieldType.Bool => row?.Bool ?? false,
        FormFieldType.Int => row?.Int ?? 0,
        FormFieldType.Long => row?.Long ?? 0L,
        FormFieldType.Float => row?.Float ?? 0d,
        FormFieldType.Decimal => row?.Decimal ?? 0m,
        FormFieldType.DateTime => row?.DateTime is { } date ? new DateTimeOffset(date) : null,
        FormFieldType.Select => row is { } value ? Key(field.Key, value.VariantId) : "",
        FormFieldType.Relation or FormFieldType.File or FormFieldType.Image => row?.ModelId ?? Guid.Empty,
        _ => null,
    };

    void Write(FormFieldDescriptor field, object? value, MetaValueEditModel row)
    {
        switch (field.Type)
        {
            case FormFieldType.String:
                row.StringShort = value as string ?? "";
                break;
            case FormFieldType.Text:
                row.StringText = value as string ?? "";
                break;
            case FormFieldType.Bool:
                row.Bool = value is true;
                break;
            case FormFieldType.Int:
                row.Int = value is int number ? number : Convert.ToInt32(value ?? 0, CultureInfo.InvariantCulture);
                break;
            case FormFieldType.Long:
                row.Long = value is long big ? big : Convert.ToInt64(value ?? 0, CultureInfo.InvariantCulture);
                break;
            case FormFieldType.Float:
                row.Float = value is double floating ? floating : Convert.ToDouble(value ?? 0, CultureInfo.InvariantCulture);
                break;
            case FormFieldType.Decimal:
                row.Decimal = value is decimal money ? money : Convert.ToDecimal(value ?? 0, CultureInfo.InvariantCulture);
                break;
            case FormFieldType.DateTime:
                row.DateTime = value switch
                {
                    DateTimeOffset offset => offset.DateTime,
                    DateTime date => date,
                    _ => null,
                };
                break;
            case FormFieldType.Select:
                row.VariantId = value is string key ? Id(field.Key, key) : Guid.Empty;
                break;
            case FormFieldType.Relation:
            case FormFieldType.File:
            case FormFieldType.Image:
                row.ModelId = value is Guid reference ? reference : Guid.Empty;
                break;
        }
    }

    /// <summary>Строка значения поля (одиночное — <c>Index == 0</c>, множественное — по индексу)</summary>
    MetaValueEditModel? Row(FormFieldDescriptor field)
        => rows.FirstOrDefault(row => row.MetaField.Key == field.Key && row.Index == 0);

    IEnumerable<MetaValueEditModel> RowsOf(FormFieldDescriptor field)
        => rows.Where(row => row.MetaField.Key == field.Key).OrderBy(row => row.Index);

    MetaValueEditModel RowForWrite(FormFieldDescriptor field)
        => Row(field) ?? NewRow(field, 0);

    MetaValueEditModel NewRow(FormFieldDescriptor field, int index)
    {
        if (Field(field.Key) is not { } meta)
            throw new InvalidOperationException($"нет определения метаполя '{field.Key}'");

        var row = new MetaValueEditModel { Id = Guid.NewGuid(), Index = index, MetaField = meta };
        rows.Add(row);
        return row;
    }

    MetaFieldEditModel? Field(string key)
        => fields.FirstOrDefault(field => field.Key == key)
           ?? rows.FirstOrDefault(row => row.MetaField.Key == key)?.MetaField;

    string Key(string fieldKey, Guid variantId)
        => variantId == Guid.Empty
            ? ""
            : Field(fieldKey)?.Variants.FirstOrDefault(variant => variant.Id == variantId)?.Key ?? "";

    IEnumerable<string> Keys(FormFieldDescriptor field, Guid[] variantIds)
        => variantIds.Select(id => Key(field.Key, id)).Where(key => key.Length > 0);

    Guid Id(string fieldKey, string variantKey)
        => Field(fieldKey)?.Variants.FirstOrDefault(variant => variant.Key == variantKey)?.Id ?? Guid.Empty;

    IEnumerable<Guid> Ids(FormFieldDescriptor field, IEnumerable<string> variantKeys)
        => variantKeys.Select(key => Id(field.Key, key)).Where(id => id != Guid.Empty);

    static bool IsList(FormFieldDescriptor field)
        => field.Multiple || field.Type == FormFieldType.SelectMany;
}
