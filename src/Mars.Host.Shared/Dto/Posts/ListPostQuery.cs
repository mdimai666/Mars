using Mars.Shared.Common;

namespace Mars.Host.Shared.Dto.Posts;

public record ListPostQuery : BasicListQuery
{
    public required string? Type { get; init; }

}
