namespace Mars.Data.OwnedTypes.Files;

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
        Width = imageInfo.Width;
        Height = imageInfo.Height;
    }
}
