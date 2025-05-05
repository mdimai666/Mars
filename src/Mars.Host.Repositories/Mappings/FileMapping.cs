using Mars.Host.Data.Entities;
using Mars.Host.Data.OwnedTypes.Files;
using Mars.Host.Shared.Dto.Files;
using Mars.Shared.Common;

namespace Mars.Host.Repositories.Mappings;

internal static class FileMapping
{
    public static FileSummary ToSummary(this FileEntity entity, ImagePreviewResolver imagePreviewResolver)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Name = entity.FileName,
            Ext = entity.FileExt,
            Size = entity.FileSize,

            IsImage = imagePreviewResolver.HostingInfo.ExtIsImage(entity.FileExt),
            IsSvg = imagePreviewResolver.HostingInfo.ExtIsSvg(entity.FileExt),
            PreviewIcon = imagePreviewResolver.ResolvePreview(entity),

            Url = imagePreviewResolver.HostingInfo.FileAbsoluteUrlFromPath(entity.FilePhysicalPath),
            UrlRelative = imagePreviewResolver.HostingInfo.FileRelativeUrlFromPath(entity.FilePhysicalPath),
        };

    public static FileListItem ToListItem(this FileEntity entity, ImagePreviewResolver imagePreviewResolver)
       => new()
       {
           Id = entity.Id,
           CreatedAt = entity.CreatedAt,
           Name = entity.FileName,
           Ext = entity.FileExt,
           Size = entity.FileSize,

           IsImage = imagePreviewResolver.HostingInfo.ExtIsImage(entity.FileExt),
           IsSvg = imagePreviewResolver.HostingInfo.ExtIsSvg(entity.FileExt),
           PreviewIcon = imagePreviewResolver.ResolvePreview(entity),

           Url = imagePreviewResolver.HostingInfo.FileAbsoluteUrlFromPath(entity.FilePhysicalPath),
           UrlRelative = imagePreviewResolver.HostingInfo.FileRelativeUrlFromPath(entity.FilePhysicalPath),

           FilePhysicalPath = entity.FilePhysicalPath,
           FileVirtualPath = entity.FileVirtualPath,
       };

    public static FileDetail ToDetail(this FileEntity entity, ImagePreviewResolver imagePreviewResolver)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Name = entity.FileName,
            Ext = entity.FileExt,
            Size = entity.FileSize,
            FilePhysicalPath = entity.FilePhysicalPath,
            FileVirtualPath = entity.FileVirtualPath,
            ModifiedAt = entity.ModifiedAt,
            UserId = entity.UserId,
            Meta = entity.Meta.ToDto(imagePreviewResolver.HostingInfo),

            IsImage = imagePreviewResolver.HostingInfo.ExtIsImage(entity.FileExt),
            IsSvg = imagePreviewResolver.HostingInfo.ExtIsSvg(entity.FileExt),
            PreviewIcon = imagePreviewResolver.ResolvePreview(entity),

            Url = imagePreviewResolver.HostingInfo.FileAbsoluteUrlFromPath(entity.FilePhysicalPath),
            UrlRelative = imagePreviewResolver.HostingInfo.FileRelativeUrlFromPath(entity.FilePhysicalPath),
        };

    public static FileEntityMetaDto ToDto(this FileEntityMeta entity, FileHostingInfo hostingInfo)
        => new()
        {
            ImageInfo = entity.ImageInfo?.ToDto(hostingInfo),
            Thumbnails = entity.Thumbnails?.ToDto(hostingInfo)
        };

    public static ImageInfoDto ToDto(this ImageInfo entity, FileHostingInfo hostingInfo)
        => new()
        {
            Height = entity.Height,
            Width = entity.Width,
        };

    public static ImageThumbnailDto ToDto(this ImageThumbnail entity, FileHostingInfo hostingInfo)
        => new()
        {
            Name = entity.Name,
            FileUrl = entity.FileUrl,
            FilePath = entity.FilePath,
            Height = entity.Height,
            Width = entity.Width
        };

    public static Dictionary<string, ImageThumbnailDto> ToDto(this IDictionary<string, ImageThumbnail> entity, FileHostingInfo hostingInfo)
        => entity.ToDictionary(s => s.Key, s => ToDto(s.Value, hostingInfo));

    public static IReadOnlyCollection<FileSummary> ToSummaryList(this IEnumerable<FileEntity> entities, ImagePreviewResolver imagePreviewResolver)
        => entities.Select(x => ToSummary(x, imagePreviewResolver)).ToArray();
    public static IReadOnlyCollection<FileListItem> ToListItemList(this IEnumerable<FileEntity> entities, ImagePreviewResolver imagePreviewResolver)
        => entities.Select(x => ToListItem(x, imagePreviewResolver)).ToArray();
    public static IReadOnlyCollection<FileDetail> ToDetailList(this IEnumerable<FileEntity> entities, ImagePreviewResolver imagePreviewResolver)
        => entities.Select(x => ToDetail(x, imagePreviewResolver)).ToArray();

    public static ListDataResult<FileSummary> ToSummaryList(this ListDataResult<FileEntity> list, ImagePreviewResolver imagePreviewResolver)
        => new(list.Items.ToSummaryList(imagePreviewResolver), list.HasMoreData, list.TotalCount);
    public static PagingResult<FileSummary> ToSummaryList(this PagingResult<FileEntity> list, ImagePreviewResolver imagePreviewResolver)
        => new(list.Items.ToSummaryList(imagePreviewResolver), list.Page, list.PageSize, list.HasMoreData, list.TotalCount);

    public static ListDataResult<FileListItem> ToListItemList(this ListDataResult<FileEntity> list, ImagePreviewResolver imagePreviewResolver)
        => new(list.Items.ToListItemList(imagePreviewResolver), list.HasMoreData, list.TotalCount);
    public static PagingResult<FileListItem> ToListItemList(this PagingResult<FileEntity> list, ImagePreviewResolver imagePreviewResolver)
        => new(list.Items.ToListItemList(imagePreviewResolver), list.Page, list.PageSize, list.HasMoreData, list.TotalCount);

    //Entity

    public static FileEntity ToEntity(this CreateFileQuery query, FileHostingInfo hostingInfo)
        => new FileEntity
        {
            FileName = query.Name,
            FileExt = hostingInfo.GetExtension(query.FilePathFromUpload),

            FilePhysicalPath = query.FilePathFromUpload,
            FileVirtualPath = query.FilePathFromUpload,
            FileSize = query.Size,
            UserId = query.UserId,

            Meta = query.Meta?.ToEntity(hostingInfo) ?? new()
        };

    public static FileEntityMeta ToEntity(this FileEntityMetaDto dto, FileHostingInfo hostingInfo)
        => new()
        {
            ImageInfo = dto.ImageInfo?.ToEntity(hostingInfo),
            Thumbnails = dto.Thumbnails?.ToEntity(hostingInfo)
        };

    public static ImageInfo ToEntity(this ImageInfoDto dto, FileHostingInfo hostingInfo)
        => new()
        {
            Height = dto.Height,
            Width = dto.Width,
        };

    public static ImageThumbnail ToEntity(this ImageThumbnailDto dto, FileHostingInfo hostingInfo)
        => new()
        {
            Name = dto.Name,
            FileUrl = dto.FileUrl,
            FilePath = dto.FilePath,
            Height = dto.Height,
            Width = dto.Width
        };

    public static Dictionary<string, ImageThumbnail> ToEntity(this IDictionary<string, ImageThumbnailDto> dto, FileHostingInfo hostingInfo)
        => dto.ToDictionary(s => s.Key, s => ToEntity(s.Value, hostingInfo));
}
