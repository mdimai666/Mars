using System.Drawing;
using Mars.Media.Abstractions.Services;
using Mars.Media.Contracts;
using PhotoSauce.MagicScaler;
using PhotoSauce.NativeCodecs.Giflib;
using PhotoSauce.NativeCodecs.Libheif;
using PhotoSauce.NativeCodecs.Libjpeg;
using PhotoSauce.NativeCodecs.Libjxl;
using PhotoSauce.NativeCodecs.Libpng;
using PhotoSauce.NativeCodecs.Libwebp;
using static Mars.Media.Contracts.Options.ImagePreviewSizeConfig;

namespace Mars.Media.Host.Services;

/// <summary>
/// Singletone.
/// </summary>
public class ImageProcessor : IImageProcessor
{
    public ImageProcessor()
    {
        CodecManager.Configure(codecs =>
        {
            codecs.UseLibwebp();
            codecs.UseLibpng();
            codecs.UseLibjpeg();
            codecs.UseGiflib();
            codecs.UseLibheif();
            codecs.UseLibjxl();
        });
    }

    public bool IsSupportImageExt(string ext)
    {
        ext = ext.TrimStart('.');
        if (ext == "svg") return false;
        return true;
    }

    ProcessImageSettings _settings(IImageConverConfig config)
    {
        var settings = new ProcessImageSettings
        {
            Width = config.Width,
            Height = config.Height,
            ResizeMode = ConvertScale(config.ResizeMode),
            EncoderOptions = config.Compression == EncoderCompression.Lossy
                ? new PhotoSauce.NativeCodecs.Libwebp.WebpLossyEncoderOptions(80)
                : new WebpLosslessEncoderOptions()
        };
        return settings;
    }

    public void ProcessImage(Stream inputImage, Stream outputImage, IImageConverConfig config)
    {
        MagicImageProcessor.ProcessImage(inputImage, outputImage, _settings(config));
    }

    public void ProcessImage(ReadOnlySpan<byte> inputImage, Stream outputImage, IImageConverConfig config)
    {
        MagicImageProcessor.ProcessImage(inputImage, outputImage, _settings(config));
    }

    PhotoSauce.MagicScaler.CropScaleMode ConvertScale(Mars.Media.Contracts.Options.ImagePreviewSizeConfig.CropScaleMode mode)
    {
        return mode switch
        {
            Mars.Media.Contracts.Options.ImagePreviewSizeConfig.CropScaleMode.Crop => PhotoSauce.MagicScaler.CropScaleMode.Crop,
            Mars.Media.Contracts.Options.ImagePreviewSizeConfig.CropScaleMode.Contain => PhotoSauce.MagicScaler.CropScaleMode.Contain,
            Mars.Media.Contracts.Options.ImagePreviewSizeConfig.CropScaleMode.Stretch => PhotoSauce.MagicScaler.CropScaleMode.Stretch,
            Mars.Media.Contracts.Options.ImagePreviewSizeConfig.CropScaleMode.Pad => PhotoSauce.MagicScaler.CropScaleMode.Pad,
            Mars.Media.Contracts.Options.ImagePreviewSizeConfig.CropScaleMode.Max => PhotoSauce.MagicScaler.CropScaleMode.Max,
            _ => throw new NotSupportedException()
        };
    }

    public Size ImageSize(Stream image)
    {
        image.Seek(0, SeekOrigin.Begin);
        using var pip = MagicImageProcessor.BuildPipeline(image, new ProcessImageSettings { });

        return new Size(pip.PixelSource.Width, pip.PixelSource.Height);
    }
}
