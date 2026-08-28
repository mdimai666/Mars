using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Contracts.MetaFields;

namespace Mars.Cms.Abstractions;

/// <summary>
/// Обогощает незаполненные MetaFields
/// </summary>
public static class MetaValuesEnricher
{
    /// <param name="metaValues"></param>
    /// <param name="metaFields"></param>
    /// <param name="contentFieldKey">Поле контента фичи: значения нет в мета-значениях (оно в posts.Content)</param>
    public static IReadOnlyCollection<MetaValueDetailDto> EnrichWithBlankMetaValuesFromMetaValues(
                                                            IEnumerable<MetaValueDetailDto> metaValues,
                                                            IReadOnlyCollection<MetaFieldDto> metaFields,
                                                            string? contentFieldKey = null)
    {

        var valuesByMfId = metaValues.GroupBy(s => s.MetaField.Id)
                                     .ToDictionary(g => g.Key, g => g.OrderBy(v => v.Index).ToList());

        var enrichMetaValues = new List<MetaValueDetailDto>(metaFields.Count);

        foreach (var mf in metaFields)
        {
            if (mf.Type == MetaFieldType.Query) continue; // вычислимое — хранимых значений нет
            if (contentFieldKey is not null && mf.Key == contentFieldKey) continue; // значение — в posts.Content

            if (valuesByMfId.TryGetValue(mf.Id, out var values))
            {
                // все строки поля (мульти-значения), без потери дубликатов
                enrichMetaValues.AddRange(values);
            }
            else if (!mf.IsMultiple)
            {
                //meta value not set. Create blank (множественные поля — ноль строк)
                var blankMetaValue = GetBlankMetaValue(mf);
                enrichMetaValues.Add(blankMetaValue);
            }
        }

        return enrichMetaValues;
    }

    public static MetaValueDetailDto GetBlankMetaValue(MetaFieldDto metaField)
    {
        var def = metaField.Default;
        return new()
        {
            Id = Guid.NewGuid(),
            Index = 0,

            Bool = def?.Bool,
            Int = def?.Int,
            Float = def?.Float,
            Decimal = def?.Decimal,
            Long = def?.Long,
            DateTime = def?.DateTime,
            ModelId = def?.ModelId,
            StringShort = metaField.Type == MetaFieldType.String ? def?.StringShort ?? ""
                        : metaField.Type == MetaFieldType.Select ? metaField.GetDefaultVariantKey()
                        : def?.StringShort,
            StringText = metaField.Type == MetaFieldType.Text ? def?.StringText ?? "" : def?.StringText,
            MetaField = metaField,
            VariantId = def?.VariantId,
            VariantsIds = def?.VariantsIds ?? []
        };
    }
}
