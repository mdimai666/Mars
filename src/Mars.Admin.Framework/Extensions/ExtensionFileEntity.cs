using Mars.Shared.Contracts.Files;

namespace AppFront.Main.Extensions;

public static class ExtensionFileEntity
{
    public static FileListItemResponse AsListItem(this FileDetailResponse entity)
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
}
