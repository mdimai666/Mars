using Mars.Contracts.Common;

namespace Mars.Media.Abstractions.Dto.Galleries;

public record ListGalleryQuery : BasicListQuery
{
    //public string? Search { get; init; }
    public required string? Type { get; init; }

}
