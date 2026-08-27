using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Dto.PostTypes;

namespace Mars.Host.Shared.Services;

/// <summary>
/// Генерация значений мета-полей при создании поста: для полей с <c>Options.generator</c>
/// дополняет <see cref="CreatePostQuery.MetaValues"/> (пустые значения, явно заданные не трогает).
/// </summary>
public interface IMetaValuesGeneratorService
{
    Task<CreatePostQuery> ApplyAsync(PostTypeDetail postType, CreatePostQuery query, CancellationToken cancellationToken);

    /// <summary>Перегенерация значений у существующих постов (номера/даты), см. <see cref="MetaValuesRegenerationMode"/>.</summary>
    Task<RegenerateMetaValuesResult> RegenerateAsync(RegenerateMetaValuesQuery query, CancellationToken cancellationToken);
}
