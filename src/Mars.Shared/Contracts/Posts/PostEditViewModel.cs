using Mars.Shared.Contracts.PostTypes;

namespace Mars.Shared.Contracts.Posts;

public record PostEditViewModel
{
    public required PostEditResponse Post { get; init; }
    public required PostTypeDetailResponse PostType { get; init; }
}
