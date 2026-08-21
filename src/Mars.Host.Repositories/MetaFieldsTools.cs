using Mars.Core.Utils;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Data.OwnedTypes.MetaFields;
using Mars.Host.Repositories.Mappings;
using Mars.Host.Shared.Dto.MetaFields;

namespace Mars.Host.Repositories;

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
                _marsDbContext.MetaFields.Add(item);
                existMetaFields.Add(item);
            }
        }
        foreach (var item in existMetaFields.Except(metaDiff.ToRemove).Except(metaDiff.ToAdd))
        {
            var q = queryDict[item.Id];
            var qe = q.ToEntity();

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
            item.Title = q.Title;
            item.Tags = q.Tags.ToList();
            item.Value = q.Value;
            item.Disable = q.Disable;

        }
    }
}
