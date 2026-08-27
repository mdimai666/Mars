using Mars.Shared.Contracts.Files;

namespace Mars.Shared.Contracts.Galleries;

public record GalleryListItemResponse
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset? ModifiedAt { get; init; }
    public required string Title { get; init; }
    public required string Type { get; init; }
    public required string Slug { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required GalleryPhotoResponse? GalleryImage { get; init; }
    public required int GalleryPhotosCount { get; init; }

}

public record GalleryDetailResponse : GalleryListItemResponse
{
    public required IReadOnlyCollection<GalleryPhotoResponse> Photos { get; init; }
}

public record GalleryPhotoResponse : FileDetailResponse
{

}
