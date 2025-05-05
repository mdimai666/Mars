using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.Host.Shared.Services;
using Mars.Options.Interfaces;
using PhotoSauce.MagicScaler;
using PhotoSauce.NativeCodecs.Giflib;
using PhotoSauce.NativeCodecs.Libheif;
using PhotoSauce.NativeCodecs.Libjpeg;
using PhotoSauce.NativeCodecs.Libjxl;
using PhotoSauce.NativeCodecs.Libpng;
using PhotoSauce.NativeCodecs.Libwebp;
using static Mars.Options.Models.ImagePreviewSizeConfig;

namespace Mars.Services;

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
            //EncoderOptions = new PhotoSauce.MagicScaler.encoderoptions
            EncoderOptions = config.Compression == EncoderCompression.Lossy
                ? new PhotoSauce.NativeCodecs.Libwebp.WebpLossyEncoderOptions(80)
                : new WebpLosslessEncoderOptions()
        };
        return settings;
    }

    IProcessImageResult _result(ProcessImageResult result)
    {
        PixelSourceStats? st = result.Stats.FirstOrDefault();

        return new Mars.Options.Models.ProcessImageResult
        {
            ProcessingTime = st?.ProcessingTime ?? 0,
            FileSize = 0,
            Width = result.Settings.Width,
            Height = result.Settings.Height,
        };
    }

    public IProcessImageResult ProcessImage(string inputImage, string outputImage, IImageConverConfig config)
    {
        var settings = _settings(config);

        var result = MagicImageProcessor.ProcessImage(inputImage, outputImage, settings);

        return _result(result);
    }

    public IProcessImageResult ProcessImage(Stream inputImage, Stream outputImage, IImageConverConfig config)
    {
        var settings = _settings(config);

        var result = MagicImageProcessor.ProcessImage(inputImage, outputImage, settings);

        return _result(result);
    }

    public IProcessImageResult ProcessImage(ReadOnlySpan<byte> inputImage, Stream outputImage, IImageConverConfig config)
    {
        var settings = _settings(config);

        var result = MagicImageProcessor.ProcessImage(inputImage, outputImage, settings);

        return _result(result);
    }

    PhotoSauce.MagicScaler.CropScaleMode ConvertScale(Options.Models.ImagePreviewSizeConfig.CropScaleMode mode)
    {
        return mode switch
        {
            Options.Models.ImagePreviewSizeConfig.CropScaleMode.Crop => PhotoSauce.MagicScaler.CropScaleMode.Crop,
            Options.Models.ImagePreviewSizeConfig.CropScaleMode.Contain => PhotoSauce.MagicScaler.CropScaleMode.Contain,
            Options.Models.ImagePreviewSizeConfig.CropScaleMode.Stretch => PhotoSauce.MagicScaler.CropScaleMode.Stretch,
            Options.Models.ImagePreviewSizeConfig.CropScaleMode.Pad => PhotoSauce.MagicScaler.CropScaleMode.Pad,
            Options.Models.ImagePreviewSizeConfig.CropScaleMode.Max => PhotoSauce.MagicScaler.CropScaleMode.Max,
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
