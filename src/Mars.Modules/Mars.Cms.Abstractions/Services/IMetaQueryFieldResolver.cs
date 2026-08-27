using Mars.Cms.Abstractions.Dto.PostTypes;

namespace Mars.Cms.Abstractions.Services;

/// <summary>
/// Батч-резолв вычислимых полей <see cref="Mars.Contracts.MetaFields.MetaFieldType.Query"/>
/// </summary>
public interface IMetaQueryFieldResolver
{
    /// <summary>
    /// Резолвит значения Query-полей типа поста для набора постов.
    /// Возвращает: ключ поля → (ИД поста → значение, массив целевых моделей)
    /// </summary>
    Task<Dictionary<string, Dictionary<Guid, object?>>> ResolveAsync(
        PostTypeDetail postType,
        IReadOnlyCollection<Guid> postIds,
        CancellationToken cancellationToken);
}
