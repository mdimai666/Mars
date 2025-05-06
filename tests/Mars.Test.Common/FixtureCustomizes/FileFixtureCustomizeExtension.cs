using AutoFixture;
using Mars.Host.Data.Entities;
using Mars.Host.Data.OwnedTypes.Files;
using Mars.Host.Shared.Dto.Files;
using Mars.Options.Models;
using Mars.Test.Common.Constants;

namespace Mars.Test.Common.FixtureCustomizes;

public static class FileFixtureCustomizeExtension
{
    public static FileEntity CreateImagePng(this IFixture fixture)
    {
        var fname = $"image-{Guid.NewGuid()}.png";

        return fixture.Build<FileEntity>()
                            .OmitAutoProperties()
                            .With(s => s.Id)
                            .With(s => s.FileName, fname)
                            .With(s => s.FileExt, "png")
                            .With(s => s.FileSize, (ulong)Random.Shared.Next(10000, 2_000_000))
                            .With(s => s.FilePhysicalPath, $"Media/{fname}")
                            .With(s => s.FileVirtualPath, $"Media/{fname}")
                            .With(s => s.CreatedAt, FixtureCustomize.DefaultCreated)
                            .With(s => s.UserId, UserConstants.TestUserId)
                            .With(s => s.Meta, fixture.CreateImageMeta(fname))
                            .Create();
    }

    public static readonly FileHostingInfo FileHostingInfo
        = new()
        {
            Backend = new Uri("http://localhost"),
            PhysicalPath = new Uri("C:\\www\\mars\\wwwRoot\\upload"),
            RequestPath = "upload"
        };

    private static FileHostingInfo _hostingInfo => FileHostingInfo;

    public static FileEntityMeta CreateImageMeta(this IFixture fixture, string fname)
    {
        var mediaOption = new MediaOption()
        {
            ImagePreviewSizeConfigs = MediaOption.DefaultImagePreviewSizeConfigs
        };

        var thumbnails = new Dictionary<string, ImageThumbnailDto>(mediaOption.ImagePreviewSizeConfigs.Length);

        var filePathFromUpload = "/2024/";

        foreach (var cfg in mediaOption.ImagePreviewSizeConfigs)
        {
            string thumbFilepath = GenerateImageThumbPath(cfg, fname, filePathFromUpload);
            string thumbFilepathAbsolutePath = _hostingInfo.FileAbsolutePath(thumbFilepath);
            var thumb = GetImageThumbnail(cfg, thumbFilepath);
            thumbnails.Add(cfg.Name, thumb);
        }

        var meta = fixture.Build<FileEntityMeta>()
                            .OmitAutoProperties()
                            .With(s => s.ImageInfo)
                            .With(s => s.Thumbnails, thumbnails.ToEntity(_hostingInfo))
                            .Create();
        return meta;
    }

    private const string _MediaThumbsDirName = "MediaThumbs";

    public static string GenerateImageThumbPath(ImagePreviewSizeConfig config, string filename, string path)
    {
        return $"{_MediaThumbsDirName}/{path}/" + $"{filename}_{config.Name}.webp";
    }

    public static ImageThumbnailDto GetImageThumbnail(ImagePreviewSizeConfig cfg, string thumbFilepath)
    {
        var thumb = new ImageThumbnailDto
        {
            FilePath = _hostingInfo.NormalizePathSlashes(thumbFilepath)!,
            FileUrl = _hostingInfo.FileRelativeUrlFromPath(thumbFilepath),
            Width = cfg.Width,
            Height = cfg.Height,
            Name = cfg.Name
        };

        return thumb;
    }

    public static FileEntityMeta ToEntity(this FileEntityMetaDto dto, FileHostingInfo hostingInfo)
        => new()
        {
            ImageInfo = dto.ImageInfo?.ToEntity(hostingInfo),
            Thumbnails = dto.Thumbnails?.ToEntity(hostingInfo)
        };

    public static ImageInfo ToEntity(this ImageInfoDto dto, FileHostingInfo hostingInfo)
        => new()
        {
            Height = dto.Height,
            Width = dto.Width,
        };

    public static ImageThumbnail ToEntity(this ImageThumbnailDto dto, FileHostingInfo hostingInfo)
        => new()
        {
            Name = dto.Name,
            FileUrl = dto.FileUrl,
            FilePath = dto.FilePath,
            Height = dto.Height,
            Width = dto.Width
        };

    public static Dictionary<string, ImageThumbnail> ToEntity(this IDictionary<string, ImageThumbnailDto> dto, FileHostingInfo hostingInfo)
        => dto.ToDictionary(s => s.Key, s => ToEntity(s.Value, hostingInfo));
}
