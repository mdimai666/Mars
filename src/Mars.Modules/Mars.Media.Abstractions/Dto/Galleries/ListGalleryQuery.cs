using Mars.Shared.Common;

namespace Mars.Host.Shared.Dto.Galleries;

public record ListGalleryQuery : BasicListQuery
{
    //public string? Search { get; init; }
    public required string? Type { get; init; }

}
