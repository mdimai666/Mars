using Mars.Core.Utils;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Data.OwnedTypes.MetaFields;
using Mars.Host.Repositories.Mappings;
using Mars.Host.Shared.Dto.MetaFields;

namespace Mars.Host.Repositories;

public static class MetaFieldsTools
{
    public static void ModifyMetaFields(MarsDbContext _marsDbContext, List<MetaFieldEntity> existMetaFields, IReadOnlyCollection<MetaFieldDto> modifyMetaFields, DateTimeOffset modifiedAt)
    {
        var metaDiff = DiffList.FindDifferencesBy(existMetaFields, modifyMetaFields.ToEntity(), s => s.Id);

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
            _marsDbContext.Entry(item).CurrentValues.SetValues(new
            {
                qe.ParentId,
                qe.Title,
                qe.Key,
                qe.Type,
                //qe.Variants,
                qe.MaxValue,
                qe.MinValue,
                qe.Description,
                qe.IsNullable,
                //qe.Default,
                qe.Order,
                qe.Tags,
                qe.Hidden,
                qe.Disabled,
                qe.ModelName,
            });
            item.ModifiedAt = modifiedAt;

            ModifyVariants(item.Variants, qe.Variants);
        }

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
