using Mars.Shared.Common;

namespace Mars.Host.Shared.Dto.PostCategories;

public record ListPostCategoryQuery : BasicListQuery
{
    public required string? Type { get; init; }
    public required string? PostTypeName { get; init; }

}
