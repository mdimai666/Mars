using System.Globalization;
using Mars.Core.Extensions;
using Mars.Core.Utils;
using Mars.Host.Data.Constants;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories.Mappings;
using Mars.Host.Shared.Dto.MetaFields;

namespace Mars.Host.Repositories;

public static class MetaValuesTools
{
    public static void ModifyMetaValues<TValue>(
        MarsDbContext _marsDbContext,
        ICollection<TValue> existMetaFields,
        IReadOnlyCollection<ModifyMetaValueDetailQuery> modifyMetaFields,
        DateTimeOffset modifiedAt)
        where TValue : MetaValueBase, new()
    {
        if (existMetaFields.Count == 0 && modifyMetaFields.Count == 0)
        {
            return;
        }

        var metaDiff = DiffList.FindDifferencesBy(existMetaFields.ToList(), modifyMetaFields.ToEntity<TValue>().ToList(), s => s.Id);

        //TODO: сделать проверку что Для каждого MetaField есть только один MetaValue

        var queryDict = modifyMetaFields.ToDictionary(s => s.Id);

        if (metaDiff.HasChanges)
        {
            foreach (var item in metaDiff.ToRemove)
            {
                existMetaFields.Remove(item);
                _marsDbContext.Set<TValue>().Remove(item);
            }
            foreach (var item in metaDiff.ToAdd)
            {
                var q = queryDict[item.Id];
                item.CreatedAt = modifiedAt;
                item.ModifiedAt = null;
                item.MetaFieldId = q.MetaFieldId;
                _marsDbContext.Set<TValue>().Add(item);
                existMetaFields.Add(item);
            }
        }
        foreach (var item in existMetaFields.Except(metaDiff.ToRemove).Except(metaDiff.ToAdd))
        {
            var q = queryDict[item.Id];
            var qe = q.ToEntity<TValue>();
            _marsDbContext.Entry(item).CurrentValues.SetValues(new
            {
                qe.Id,
                qe.Index,
                qe.Type,

                qe.Bool,
                qe.Int,
                qe.Float,
                qe.Decimal,
                qe.Long,
                qe.StringText,
                qe.StringShort,
                qe.DateTime,
                qe.VariantId,
                qe.VariantsIds,
                qe.ModelId,
            });
            item.ModifiedAt = modifiedAt;

        }

    }

    /// <summary>
    /// Переносит значения между колонками при смене типа поля, где это возможно
    /// (числа/строки/даты; варианты и связи не мигрируют). Непереносимые значения
    /// обнуляются — пользователь предупреждается на клиенте.
    /// </summary>
    public static void MigrateValuesOnFieldTypeChange(MarsDbContext ctx, MetaFieldEntity field, EMetaFieldType oldType, EMetaFieldType newType)
    {
        if (oldType == newType) return;

        List<MetaValueBase> values;
        if (field.PostTypeId is not null)
            values = ctx.PostMetaValues.Where(v => v.MetaFieldId == field.Id).Cast<MetaValueBase>().ToList();
        else if (field.UserTypeId is not null)
            values = ctx.UserMetaValues.Where(v => v.MetaFieldId == field.Id).Cast<MetaValueBase>().ToList();
        else if (field.PostCategoryTypeId is not null)
            values = ctx.PostCategoryMetaValues.Where(v => v.MetaFieldId == field.Id).Cast<MetaValueBase>().ToList();
        else
            return;

        foreach (var v in values)
        {
            var source = ReadValueAsText(v, oldType);
            ResetValueColumns(v);
            WriteValueFromText(v, newType, source);
            v.Type = newType;
            v.ModifiedAt = DateTimeOffset.Now;
        }
    }

    static string? ReadValueAsText(MetaValueBase v, EMetaFieldType type) => type switch
    {
        EMetaFieldType.String => v.StringShort,
        EMetaFieldType.Text => v.StringText,
        EMetaFieldType.Bool => v.Bool?.ToString(),
        EMetaFieldType.Int => v.Int?.ToString(CultureInfo.InvariantCulture),
        EMetaFieldType.Long => v.Long?.ToString(CultureInfo.InvariantCulture),
        EMetaFieldType.Float => v.Float?.ToString(CultureInfo.InvariantCulture),
        EMetaFieldType.Decimal => v.Decimal?.ToString(CultureInfo.InvariantCulture),
        EMetaFieldType.DateTime => v.DateTime?.ToString("O", CultureInfo.InvariantCulture),
        _ => null,
    };

    static void ResetValueColumns(MetaValueBase v)
    {
        v.Bool = null;
        v.Int = null;
        v.Float = null;
        v.Decimal = null;
        v.Long = null;
        v.StringText = null;
        v.StringShort = null;
        v.DateTime = null;
        v.VariantId = null;
        v.VariantsIds = [];
        v.ModelId = null;
    }

    static void WriteValueFromText(MetaValueBase v, EMetaFieldType type, string? source)
    {
        if (source is null) return;

        switch (type)
        {
            case EMetaFieldType.String:
                v.StringShort = source.Left(EntityDefaultConstants.DefaultShortValueMaxLength);
                break;
            case EMetaFieldType.Text:
                v.StringText = source;
                break;
            case EMetaFieldType.Bool:
                if (bool.TryParse(source, out var b)) v.Bool = b;
                break;
            case EMetaFieldType.Int:
                if (int.TryParse(source, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) v.Int = i;
                break;
            case EMetaFieldType.Long:
                if (long.TryParse(source, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)) v.Long = l;
                break;
            case EMetaFieldType.Float:
                if (double.TryParse(source, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)) v.Float = f;
                break;
            case EMetaFieldType.Decimal:
                if (decimal.TryParse(source, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)) v.Decimal = d;
                break;
            case EMetaFieldType.DateTime:
                if (DateTime.TryParse(source, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt)) v.DateTime = dt;
                break;
            default:
                break; // варианты/связи не мигрируют
        }
    }
}
