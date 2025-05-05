using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Mars.Options.Interfaces;
using Mars.Core.Extensions;

namespace Mars.Options.Models;

[Display(Name = "Настройки Api")]
public class MediaOption
{
    [Display(Name = "Maximum InputFile Size")]
    public ulong MaximumInputFileSize { get; set; } = 100 * 1024 * 1024;
    [Display(Name = "Maximum InputFile Size")]
    [JsonIgnore, Newtonsoft.Json.JsonIgnore]

    public long MaximumInputFileSizeSetter { get => (long)MaximumInputFileSize; set => MaximumInputFileSize = (ulong)value; }

    [Display(Name = "Is Allow All File Types")]
    public bool IsAllowAllFileTypes { get; set; }

    public static readonly string DefaultAllowedExtensions = ".png,.jpg,.jpeg,.webp,.doc,.docx,.ppt,.pptx,.xls,.xlsx,.pdf";

    string[] _allowedFileExtensions
        = DefaultAllowedExtensions.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    [Display(Name = "Allowed File Extensions")]
    public string[] AllowedFileExtensions
    {
        get => _allowedFileExtensions;
        set { _allowedFileExtensions = value; UpdAllowDict(); }
    }

    [JsonIgnore, Newtonsoft.Json.JsonIgnore]
    [Display(Name = "Allowed File Extensions")]
    public string AllowedFileExtensionsSetter
    {
        get => _allowedFileExtensions.JoinStr(",");
        set => AllowedFileExtensions
            = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    static Dictionary<string, string> _allowedDict = new();

    [Display(Name = "Image Preview Size Configs")]
    public ImagePreviewSizeConfig[] ImagePreviewSizeConfigs { get; set; } = DefaultImagePreviewSizeConfigs;

    public static readonly ImagePreviewSizeConfig[] DefaultImagePreviewSizeConfigs =
    {
        new ImagePreviewSizeConfig {
            Name = "xs", Width=130, Height=80, ResizeMode = ImagePreviewSizeConfig.CropScaleMode.Max },
        new ImagePreviewSizeConfig {
            Name = "sm", Width=300, Height=225, ResizeMode = ImagePreviewSizeConfig.CropScaleMode.Max },
        new ImagePreviewSizeConfig {
            Name = "md", Width=500, Height=500, ResizeMode = ImagePreviewSizeConfig.CropScaleMode.Max },
    };

    public bool IsAllowExtension(string dotExt)
    {
        if (IsAllowAllFileTypes) return true;

        return _allowedDict.ContainsKey(dotExt);
    }

    void UpdAllowDict()
    {
        _allowedDict = _allowedFileExtensions.ToDictionary(s => s, s => s);
    }

    [Display(Name = "Is Auto Resize Upload Image")]
    public bool IsAutoResizeUploadImage { get; set; } = true;

    [Display(Name = "Auto Resize Upload Image Config")]
    public ImagePreviewSizeConfig AutoResizeUploadImageConfig { get; set; }
        = new() { Name = "_", Width = 1200, Height = 1200, ResizeMode = ImagePreviewSizeConfig.CropScaleMode.Max };
}

public class ImagePreviewSizeConfig : IImageConverConfig
{
    [Required]
    public string Name { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }

    public CropScaleMode ResizeMode { get; set; }

    public EncoderCompression Compression { get; set; }

    //public enum ImageResizeMode
    //{
    //    Fit,
    //    Cover
    //}

    public enum EncoderCompression
    {
        Lossy,
        Lossless
    }

    public enum CropScaleMode
    {
        /// <summary>
        /// Preserve the aspect ratio of the input image. 
        /// Crop if necessary to fit the output dimensions.
        /// <para>
        /// Сохраните соотношение сторон входного изображения. 
        /// Обрежьте, если необходимо, чтобы соответствовать выходным размерам.
        /// </para>
        /// </summary>
        Crop,

        /// <summary>
        /// Preserve the aspect ratio of the input image. 
        /// Reduce one of the output dimensions if necessary to preserve the ratio.
        /// <para>
        /// Сохраните соотношение сторон входного изображения.
        /// При необходимости уменьшите одно из выходных измерений, чтобы сохранить соотношение.
        /// </para>
        /// </summary>
        Contain,

        /// <summary>
        /// Stretch the image on one axis if necessary to fill the output dimensions.
        /// <para>
        /// При необходимости растяните изображение по одной оси, чтобы заполнить выходные размеры.
        /// </para>
        /// </summary>
        Stretch,

        /// <summary>
        /// Preserve the aspect ratio of the input image. 
        /// Fill any undefined pixels with the PhotoSauce.MagicScaler.ProcessImageSettings.MatteColor.
        /// <para>
        /// Сохраните соотношение сторон входного изображения.
        /// Заполните все неопределенные пиксели PhotoSauce.MagicScaler.ProcessImageSettings.MatteColor.
        /// </para>
        /// </summary>
        Pad,

        /// <summary>
        /// Preserve the aspect ratio of the input image. 
        /// Reduce one or both of the output dimensions if necessary to preserve the ratio but never enlarge.
        /// <para>
        /// Сохраните соотношение сторон входного изображения.
        /// При необходимости уменьшите один или оба выходных размера, чтобы сохранить соотношение, но никогда не увеличивайте.
        /// </para>
        /// </summary>
        Max
    }
}
