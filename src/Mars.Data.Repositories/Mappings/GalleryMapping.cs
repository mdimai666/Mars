using Mars.Data.Entities;
using Mars.Media.Abstractions.Dto.Files;
using Mars.Media.Abstractions.Dto.Galleries;

namespace Mars.Data.Repositories.Mappings;

internal static class GalleryMapping
{
    public static GallerySummary ToSummary(this PostEntity entity, (GalleryPhoto galleryPhoto, int galleryPhotosCount) a)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt,
            Title = entity.Title,
            Type = entity.PostType!.TypeName,
            Tags = entity.Tags,
            Slug = entity.Slug,

            GalleryImage = a.galleryPhoto,
            GalleryPhotosCount = a.galleryPhotosCount
        };

    public static GalleryDetail ToDetail(this PostEntity entity,
                                            (GalleryPhoto galleryPhoto,
                                            int galleryPhotosCount,
                                            IReadOnlyCollection<GalleryPhoto> galleryPhotos) a)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            Type = entity.PostType!.TypeName,
            Slug = entity.Slug,
            Tags = entity.Tags,
            ModifiedAt = entity.ModifiedAt,

            GalleryImage = a.galleryPhoto,
            GalleryPhotosCount = a.galleryPhotosCount,
            Photos = a.galleryPhotos,
        };


    public static GalleryPhoto ToGalleryPhoto(this FileDetail entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt,
            Name = entity.Name,
            Ext = entity.Ext,
            FilePhysicalPath = entity.FilePhysicalPath,
            FileVirtualPath = entity.FileVirtualPath,
            IsImage = entity.IsImage,
            IsSvg = entity.IsSvg,
            Size = entity.Size,
            Url = entity.Url,
            UrlRelative = entity.UrlRelative,
            UserId = entity.UserId,
            Meta = entity.Meta,
            PreviewIcon = entity.PreviewIcon,
        };

    public static IReadOnlyCollection<GallerySummary> ToSummaryList(this IEnumerable<(PostEntity post, GalleryPhoto galleryPhoto, int galleryPhotosCount)> entities)
        => entities.Select(x => ToSummary(x.post, (x.galleryPhoto, x.galleryPhotosCount))).ToArray();
}
