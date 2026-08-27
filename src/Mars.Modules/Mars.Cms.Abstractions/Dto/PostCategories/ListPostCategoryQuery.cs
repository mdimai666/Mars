using Mars.Contracts.Common;

namespace Mars.Cms.Abstractions.Dto.PostCategories;

public record ListPostCategoryQuery : BasicListQuery
{
    public required string? Type { get; init; }
    public required string? PostTypeName { get; init; }

}
