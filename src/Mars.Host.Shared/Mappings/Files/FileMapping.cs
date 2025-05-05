using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.Files;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Files;

namespace Mars.Host.Shared.Mappings.Files;

public static class FileMapping
{
    //public static FileSummaryResponse ToResponse(this FileSummary entity)
    //    => new()
    //    {
    //        Id = entity.Id,
    //        CreatedAt = entity.CreatedAt,
    //        Name = entity.Name,
    //        Ext = entity.Ext,
    //        IsImage = entity.IsImage,
    //        IsSvg = entity.IsSvg,
    //        PreviewIcon = entity.PreviewIcon,
    //        Size = entity.Size,
    //        Url = entity.Url,
    //        UrlRelative = entity.UrlRelative
    //    };

    public static FileListItemResponse ToResponse(this FileListItem entity)
       => new()
       {
           Id = entity.Id,
           CreatedAt = entity.CreatedAt,
           Name = entity.Name,
           Ext = entity.Ext,
           IsImage = entity.IsImage,
           IsSvg = entity.IsSvg,
           PreviewIcon = entity.PreviewIcon,
           Size = entity.Size,
           Url = entity.Url,
           UrlRelative = entity.UrlRelative,
           FilePhysicalPath = entity.FilePhysicalPath,
           FileVirtualPath = entity.FileVirtualPath,
       };

    public static FileDetailResponse ToResponse(this FileDetail entity)
       => new()
       {
           Id = entity.Id,
           CreatedAt = entity.CreatedAt,
           Name = entity.Name,
           Ext = entity.Ext,
           IsImage = entity.IsImage,
           IsSvg = entity.IsSvg,
           PreviewIcon = entity.PreviewIcon,
           Size = entity.Size,
           Url = entity.Url,
           UrlRelative = entity.UrlRelative,
           FilePhysicalPath = entity.FilePhysicalPath,
           FileVirtualPath = entity.FileVirtualPath,
           ModifiedAt = entity.ModifiedAt,
           UserId = entity.UserId,
           Meta = entity.Meta.ToResponse()
       };

    public static FileEntityMetaResponse ToResponse(this FileEntityMetaDto entity)
       => new()
       {
           ImageInfo = entity.ImageInfo?.ToResponse(),
           Thumbnails = entity.Thumbnails?.ToResponse()
       };

    public static ImageInfoResponse ToResponse(this ImageInfoDto entity)
       => new()
       {
           Height = entity.Height,
           Width = entity.Width,
       };

    public static ImageThumbnailResponse ToResponse(this ImageThumbnailDto entity)
       => new()
       {
           Name = entity.Name,
           FileUrl = entity.FileUrl,
           FilePath = entity.FilePath,
           Width = entity.Width,
           Height = entity.Height,
       };

    public static Dictionary<string, ImageThumbnailResponse> ToResponse(this IDictionary<string, ImageThumbnailDto> entity)
        => entity.ToDictionary(s => s.Key, s => ToResponse(s.Value));

    public static ListDataResult<FileListItemResponse> ToResponse(this ListDataResult<FileListItem> items)
        => items.ToMap(ToResponse);

    public static PagingResult<FileListItemResponse> ToResponse(this PagingResult<FileListItem> items)
        => items.ToMap(ToResponse);

}
