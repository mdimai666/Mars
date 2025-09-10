using static Mars.Options.Models.ImagePreviewSizeConfig;

namespace Mars.Options.Interfaces;

public interface IImageConverConfig
{
    public int Width { get; set; }
    public int Height { get; set; }
    public CropScaleMode ResizeMode { get; set; }
    public EncoderCompression Compression { get; set; }
}

public interface IProcessImageResult
{
    public int Width { get; }
    public int Height { get; }
    //public bool HasAlpha { get; }
    public long FileSize { get; }
    public double ProcessingTime { get; }

}
public class ImageConverConfig : IImageConverConfig
{
    public required int Width { get; set; }
    public required int Height { get; set; }
    public required CropScaleMode ResizeMode { get; set; }
    public required EncoderCompression Compression { get; set; }
}
