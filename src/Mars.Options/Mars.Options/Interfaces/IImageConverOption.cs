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