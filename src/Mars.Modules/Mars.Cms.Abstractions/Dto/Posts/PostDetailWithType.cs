using Mars.Host.Shared.Dto.PostTypes;

namespace Mars.Host.Shared.Dto.Posts;

public record PostDetailWithType : PostDetail
{
    public required PostTypeDetail PostType { get; init; }
}
