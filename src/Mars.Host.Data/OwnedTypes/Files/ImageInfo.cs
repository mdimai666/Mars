namespace Mars.Host.Data.OwnedTypes.Files;

// [Jsonb]
public class ImageInfo
{
    public int Width { get; set; }
    public int Height { get; set; }

    public ImageInfo()
    {

    }

    public ImageInfo(ImageInfo imageInfo)
    {
        this.Width = imageInfo.Width;
        this.Height = imageInfo.Height;
    }
}
