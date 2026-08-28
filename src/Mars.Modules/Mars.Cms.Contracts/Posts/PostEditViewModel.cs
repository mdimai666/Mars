using Mars.Cms.Contracts.PostTypes;

namespace Mars.Cms.Contracts.Posts;

public record PostEditViewModel
{
    public required PostEditResponse Post { get; init; }
    public required PostTypeDetailResponse PostType { get; init; }
}
