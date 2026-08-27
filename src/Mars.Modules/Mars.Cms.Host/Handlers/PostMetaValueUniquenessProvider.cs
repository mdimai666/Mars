using Mars.Data.Contexts;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Handlers;

namespace Mars.Cms.Host.Handlers;

/// <summary>
/// Уникальность значений мета-полей постов (домен <see cref="MetaValueOwnerCatalog.Post"/>)
/// </summary>
internal class PostMetaValueUniquenessProvider(MarsDbContext marsDbContext) : IMetaValueUniquenessProvider
{
    public ValueTask<bool> IsOccupiedAsync(MetaFieldDto field, object? value, Guid? excludeOwnerId, CancellationToken cancellationToken)
    {
        var values = marsDbContext.PostMetaValues.Where(v => v.MetaFieldId == field.Id);
        if (excludeOwnerId is Guid ownerId)
            values = values.Where(v => v.PostId != ownerId);

        return MetaValueUniquenessTool.CheckAsync(values, field, value, cancellationToken);
    }
}
