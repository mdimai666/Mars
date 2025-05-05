using Mars.Core.Utils;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories.Mappings;
using Mars.Host.Shared.Dto.MetaFields;

namespace Mars.Host.Repositories;

public class MetaValuesTools
{
    public static void ModifyMetaValues(
        MarsDbContext _marsDbContext,
        List<MetaValueEntity> existMetaFields,
        IReadOnlyCollection<ModifyMetaValueDetailQuery> modifyMetaFields,
        DateTimeOffset modifiedAt)
    {
        if (existMetaFields.Count == 0 && modifyMetaFields.Count == 0)
        {
            return;
        }

        var metaFieldDict = existMetaFields.Select(x => x.MetaField)
                                            .DistinctBy(s=>s.Id)
                                            .ToDictionary(x => x.Id);

        var metaDiff = DiffList.FindDifferencesBy(existMetaFields, modifyMetaFields.ToEntity().ToList(), s => s.Id);

        //TODO: сделать проверку что Для каждого MetaField есть только один MetaValue

        var queryDict = modifyMetaFields.ToDictionary(s => s.Id);

        if (metaDiff.HasChanges)
        {
            foreach (var item in metaDiff.ToRemove)
            {
                existMetaFields.Remove(item);
                _marsDbContext.MetaValues.Remove(item);
            }
            foreach (var item in metaDiff.ToAdd)
            {
                var q = queryDict[item.Id];
                item.CreatedAt = modifiedAt;
                item.ModifiedAt = null;
                item.MetaFieldId = q.MetaFieldId;
                _marsDbContext.MetaValues.Add(item);
                existMetaFields.Add(item);
            }
        }
        foreach (var item in existMetaFields.Except(metaDiff.ToRemove).Except(metaDiff.ToAdd))
        {
            var q = queryDict[item.Id];
            var qe = q.ToEntity();
            _marsDbContext.Entry(item).CurrentValues.SetValues(new
            {
                qe.Id,
                qe.ParentId,
                qe.Index,
                qe.Type,

                qe.Bool,
                qe.Int,
                qe.Float,
                qe.Decimal,
                qe.Long,
                qe.StringText,
                qe.StringShort,
                qe.NULL,
                qe.DateTime,
                qe.VariantId,
                qe.VariantsIds,
                qe.ModelId,
            });
            item.ModifiedAt = modifiedAt;

        }

    }
}
