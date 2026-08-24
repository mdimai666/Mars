using Mars.Host.Data.Contexts;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Services;

namespace Mars.Host.Handlers;

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
