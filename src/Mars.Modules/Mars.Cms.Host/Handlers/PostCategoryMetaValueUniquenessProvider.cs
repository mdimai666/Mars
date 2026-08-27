using Mars.Data.Contexts;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Handlers;

namespace Mars.Cms.Host.Handlers;

/// <summary>
/// Уникальность значений мета-полей категорий (домен <see cref="MetaValueOwnerCatalog.PostCategory"/>)
/// </summary>
internal class PostCategoryMetaValueUniquenessProvider(MarsDbContext marsDbContext) : IMetaValueUniquenessProvider
{
    public ValueTask<bool> IsOccupiedAsync(MetaFieldDto field, object? value, Guid? excludeOwnerId, CancellationToken cancellationToken)
    {
        var values = marsDbContext.PostCategoryMetaValues.Where(v => v.MetaFieldId == field.Id);
        if (excludeOwnerId is Guid ownerId)
            values = values.Where(v => v.PostCategoryId != ownerId);

        return MetaValueUniquenessTool.CheckAsync(values, field, value, cancellationToken);
    }
}
