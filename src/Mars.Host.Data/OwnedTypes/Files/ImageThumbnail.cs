namespace Mars.Host.Data.OwnedTypes.Files;

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
        this.Name = thumb.Name;
        this.Width = thumb.Width;
        this.Height = thumb.Height;
        this.FilePath = thumb.FilePath;
        this.FileUrl = thumb.FileUrl;
    }
}
