using Mars.Shared.Common;
using Mars.Shared.Contracts.Posts;

namespace Mars.Host.Shared.Dto.Posts;

public record ListPostQuery : BasicListQuery
{
    public required string? Type { get; init; }
    public bool IncludeCategory { get; init; }
    public Guid? CategoryId { get; init; }
    /// <summary>
    /// включить дочерние категории (потомков)
    /// </summary>
    public bool FilterIncludeDescendantsCategories { get; init; }

    /// <summary>Фильтры колонок грида</summary>
    public IReadOnlyCollection<PostGridFilter>? Filters { get; init; }
}
