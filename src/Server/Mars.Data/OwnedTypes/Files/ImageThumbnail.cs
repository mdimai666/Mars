namespace Mars.Data.OwnedTypes.Files;

public class ImageThumbnail
{
    public string Name { get; set; } = default!;
    public int Width { get; set; }
    public int Height { get; set; }
    public string FilePath { get; set; } = default!;
    public string FileUrl { get; set; } = default!;

    public ImageThumbnail()
    {

    }

    public ImageThumbnail(ImageThumbnail thumb)
    {
        Name = thumb.Name;
        Width = thumb.Width;
        Height = thumb.Height;
        FilePath = thumb.FilePath;
        FileUrl = thumb.FileUrl;
    }
}
