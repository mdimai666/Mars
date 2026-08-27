using Mars.Contracts.PostTypes;

namespace Mars.Contracts.Posts;

public record PostEditViewModel
{
    public required PostEditResponse Post { get; init; }
    public required PostTypeDetailResponse PostType { get; init; }
}
