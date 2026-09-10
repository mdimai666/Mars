using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Services;
using Mars.Data.Contexts;

namespace Mars.Cms.Host.Handlers;

/// <summary>
/// Уникальность значений мета-полей пользователей (домен <see cref="MetaValueOwnerCatalog.User"/>)
/// </summary>
internal class UserMetaValueUniquenessProvider(MarsDbContext marsDbContext) : IMetaValueUniquenessProvider
{
    public ValueTask<bool> IsOccupiedAsync(MetaFieldDto field, object? value, Guid? excludeOwnerId, CancellationToken cancellationToken)
    {
        var values = marsDbContext.UserMetaValues.Where(v => v.MetaFieldId == field.Id);
        if (excludeOwnerId is Guid ownerId)
            values = values.Where(v => v.UserId != ownerId);

        return MetaValueUniquenessTool.CheckAsync(values, field, value, cancellationToken);
    }
}
