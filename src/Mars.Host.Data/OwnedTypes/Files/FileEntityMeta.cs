namespace Mars.Host.Data.OwnedTypes.Files;

// [Jsonb]
public class FileEntityMeta
{
    public ImageInfo? ImageInfo { get; set; }
    public Dictionary<string, ImageThumbnail>? Thumbnails { get; set; }

    //public FileEntityMeta()
    //{

    //}

    //public FileEntityMeta(FileEntityMeta meta)
    //{
    //    this.ImageInfo = new(meta.ImageInfo);
    //    this.Thumbnails = new(meta.Thumbnails);
    //}
}
