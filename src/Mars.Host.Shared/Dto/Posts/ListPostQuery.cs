using Mars.Shared.Common;

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
}
