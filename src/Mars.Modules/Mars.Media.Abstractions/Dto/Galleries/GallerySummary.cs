using Mars.Core.Interfaces;
using Mars.Host.Shared.Dto.Files;

namespace Mars.Host.Shared.Dto.Galleries;

public record GallerySummary : IHasId
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset? ModifiedAt { get; init; }
    public required string Title { get; init; }
    public required string Type { get; init; }
    public required string Slug { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required GalleryPhoto? GalleryImage { get; init; }
    public required int GalleryPhotosCount { get; init; }
}

public record GalleryDetail : GallerySummary
{
    public required IReadOnlyCollection<GalleryPhoto> Photos { get; init; }
}

public record GalleryPhoto : FileDetail
{

}
