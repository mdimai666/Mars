using Mars.Cms.Contracts.Posts;
using Mars.Contracts.Common;

namespace Mars.Cms.Abstractions.Dto.Posts;

public record ListPostQuery : BasicListQuery
{
    public required string? Type { get; init; }
    public bool IncludeCategory { get; init; }
    public Guid? CategoryId { get; init; }
    /// <summary>
    /// включить дочерние категории (потомков)
    /// </summary>
    public bool FilterIncludeDescendantsCategories { get; init; }

    /// <summary>Вернуть только посты с этими Id (таблицы выбранных элементов, секции детей)</summary>
    public IReadOnlyCollection<Guid>? Ids { get; init; }

    /// <summary>Фильтры колонок грида</summary>
    public IReadOnlyCollection<PostGridFilter>? Filters { get; init; }
}
