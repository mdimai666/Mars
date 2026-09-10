using System.Drawing;
using Mars.Media.Contracts;

namespace Mars.Media.Abstractions.Services;

public interface IImageProcessor
{
    public void ProcessImage(Stream inputImage, Stream outputImage, IImageConverConfig config);
    public void ProcessImage(ReadOnlySpan<byte> inputImage, Stream outputImage, IImageConverConfig config);
    public bool IsSupportImageExt(string ext);
    public Size ImageSize(Stream image);

}
