using Mars.Cms.Abstractions.Dto.MetaFields;

namespace Mars.Cms.Abstractions.Services;

/// <summary>
/// Доступ к значениям модели-владельца для правила <c>unique</c> —
/// занято ли значение у другого владельца в пределах типа.
/// Регистрируется keyed-DI по ключу модели (<see cref="MetaValueOwnerCatalog"/>)
/// </summary>
public interface IMetaValueUniquenessProvider
{
    /// <summary>True, если значение поля уже занято другим владельцем.
    /// excludeOwnerId — ид сохраняемого владельца (исключается при обновлении)</summary>
    ValueTask<bool> IsOccupiedAsync(MetaFieldDto field, object? value, Guid? excludeOwnerId, CancellationToken cancellationToken);
}
