using Mars.Host.Data.Entities;
using Mars.Host.Shared.Dto.Files;

namespace Mars.Host.Repositories.Helpers;

public class ImagePreviewResolver(ImagePreviewConfig config, FileHostingInfo hostingInfo)
{
    public FileHostingInfo HostingInfo { get; } = hostingInfo;

    public string ResolvePreview(FileEntity file)
    {
        var isImage = HostingInfo.ExtIsImage(file.FileExt);

        if(isImage && file.Meta is not null && file.Meta.Thumbnails is not null)
        {
            if (file.Meta.Thumbnails.TryGetValue(config.PrefererImageSize, out var thumbnail))
            {
                return thumbnail.FileUrl;
            }
            else if (HostingInfo.ExtIsSvg(file.FileExt) && file.FileSize < 100_000)
            {
                var first = file.Meta.Thumbnails.Values.FirstOrDefault();
                if(first is not null)
                {
                    return first.FileUrl;
                }
            }
        }

        return HostingInfo.PreviewIconUrl(file.FileExt);
    }
}
