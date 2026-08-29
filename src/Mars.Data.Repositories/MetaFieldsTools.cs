using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Utils;
using Mars.Core.Utils;
using Mars.Data.Contexts;
using Mars.Data.Entities;
using Mars.Data.OwnedTypes.MetaFields;
using Mars.Data.Repositories.Mappings;

namespace Mars.Data.Repositories;

public static class MetaFieldsTools
{
    public static void ModifyMetaFields(MarsDbContext _marsDbContext, ICollection<MetaFieldEntity> existMetaFields, IReadOnlyCollection<MetaFieldDto> modifyMetaFields, DateTimeOffset modifiedAt)
    {
        var metaDiff = DiffList.FindDifferencesBy(existMetaFields.ToList(), modifyMetaFields.ToEntity(), s => s.Id);

        var queryDict = modifyMetaFields.ToDictionary(s => s.Id);

        if (metaDiff.HasChanges)
        {
            foreach (var item in metaDiff.ToRemove)
            {
                existMetaFields.Remove(item);
                _marsDbContext.MetaFields.Remove(item);
            }
            foreach (var item in metaDiff.ToAdd)
            {
                var q = queryDict[item.Id];
                item.CreatedAt = modifiedAt;
                item.ModifiedAt = null;
                EnsureVariantKeys(item.Variants);
                _marsDbContext.MetaFields.Add(item);
                existMetaFields.Add(item);
            }
        }
        foreach (var item in existMetaFields.Except(metaDiff.ToRemove).Except(metaDiff.ToAdd))
        {
            var q = queryDict[item.Id];
            var qe = q.ToEntity();
            EnsureVariantKeys(qe.Variants);

            // смена типа поля: перенос значений между колонками, где это возможно
            if (item.Type != qe.Type)
            {
                MetaValuesTools.MigrateValuesOnFieldTypeChange(_marsDbContext, item, item.Type, qe.Type);
            }

            _marsDbContext.Entry(item).CurrentValues.SetValues(new
            {
                qe.Title,
                qe.Key,
                qe.Type,
                //qe.Variants,
                qe.MaxValue,
                qe.MinValue,
                qe.Description,
                qe.IsNullable,
                qe.IsMultiple,
                qe.Order,
                qe.Tags,
                qe.Hidden,
                qe.Disabled,
                qe.ModelName,
                qe.Options,
            });
            // owned-типы через CurrentValues не обновляются
            SetDefault(item, qe.Default);
            item.ModifiedAt = modifiedAt;

            ModifyVariants(item.Variants, qe.Variants);
        }

    }

    static void SetDefault(MetaFieldEntity entity, MetaFieldDefaultValue? newValue)
    {
        if (newValue is null)
        {
            entity.Default = null;
            return;
        }

        entity.Default ??= new MetaFieldDefaultValue();
        entity.Default.Bool = newValue.Bool;
        entity.Default.Int = newValue.Int;
        entity.Default.Float = newValue.Float;
        entity.Default.Decimal = newValue.Decimal;
        entity.Default.Long = newValue.Long;
        entity.Default.StringText = newValue.StringText;
        entity.Default.StringShort = newValue.StringShort;
        entity.Default.DateTime = newValue.DateTime;
        entity.Default.VariantId = newValue.VariantId;
        entity.Default.VariantsIds = newValue.VariantsIds;
        entity.Default.ModelId = newValue.ModelId;
    }

    static void ModifyVariants(List<MetaFieldVariant> entityVariants, List<MetaFieldVariant> newVariants)
    {
        var statusDiff = DiffList.FindDifferencesBy(entityVariants, newVariants, s => s.Id);
        if (statusDiff.HasChanges)
        {
            foreach (var item in statusDiff.ToRemove) entityVariants.Remove(item);
            foreach (var item in statusDiff.ToAdd)
            {
                entityVariants.Add(item);
            }
        }
        foreach (var item in entityVariants.Except(statusDiff.ToRemove).Except(statusDiff.ToAdd))
        {
            var q = newVariants.First(s => s.Id == item.Id);
            item.Key = q.Key;
            item.Title = q.Title;
            item.Tags = q.Tags.ToList();
            item.Value = q.Value;
            item.Disable = q.Disable;

        }
    }

    /// <summary>
    /// Обеспечивает варианты непустыми уникальными ключами: нормализует заданные,
    /// пустые генерирует из Title (backfill), коллизии разрешает суффиксами.
    /// </summary>
    public static void EnsureVariantKeys(List<MetaFieldVariant> variants)
    {
        if (variants.Count == 0) return;

        foreach (var variant in variants)
        {
            var key = MetaFieldKeyNormalizer.Normalize(variant.Key);
            if (key.Length == 0)
                key = MetaFieldKeyNormalizer.Normalize(variant.Title);
            if (key.Length == 0)
                key = $"variant_{variant.Id:N}";

            variant.Key = key;
        }

        var taken = new HashSet<string>(StringComparer.Ordinal);
        foreach (var variant in variants)
        {
            var candidate = variant.Key;
            var suffix = 2;
            while (!taken.Add(candidate))
            {
                candidate = $"{variant.Key}_{suffix}";
                suffix++;
            }
            variant.Key = candidate;
        }
    }
}
