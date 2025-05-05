using System.Drawing;
using Mars.Options.Interfaces;

namespace Mars.Host.Shared.Services;

public interface IImageProcessor
{
    public IProcessImageResult ProcessImage(string inputImage, string outputImage, IImageConverConfig config);
    public IProcessImageResult ProcessImage(Stream inputImage, Stream outputImage, IImageConverConfig config);
    public IProcessImageResult ProcessImage(ReadOnlySpan<byte> inputImage, Stream outputImage, IImageConverConfig config);
    public bool IsSupportImageExt(string ext);
    public Size ImageSize(Stream image);

}

