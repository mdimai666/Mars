using Mars.Cms.Abstractions.Dto.PostTypes;

namespace Mars.Cms.Abstractions.Dto.Posts;

public record PostDetailWithType : PostDetail
{
    public required PostTypeDetail PostType { get; init; }
}
