using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mars.Core.Utils;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Handlers;
using Mars.Host.Shared.Services;
using Mars.Options.Interfaces;
using Mars.Options.Models;

namespace Mars.Host.Handlers;

internal class FaviconGeneratorHandler : IFaviconGeneratorHandler
{
    private readonly IFileStorage _fileStorage;
    private readonly IFileService _fileService;
    private readonly IImageProcessor _imageProcessor;

    public FaviconGeneratorHandler(IFileStorage fileStorage, IFileService fileService, IImageProcessor imageProcessor)
    {
        _fileStorage = fileStorage;
        _fileService = fileService;
        _imageProcessor = imageProcessor;
    }

    public async Task<string> Handle(FaviconOption faviconOption, string appName, CancellationToken cancellationToken)
    {
        if (faviconOption.FaviconSourceImageId == Guid.Empty)
            throw new ArgumentException("Favicon source file ID is not set");

        var file = await _fileService.GetDetail(faviconOption.FaviconSourceImageId, cancellationToken)
                        ?? throw new FileNotFoundException("Favicon source file not found");

        var faviconFilesDir = "favicons/";
        var faviconFilesUrl = "/upload/" + faviconFilesDir.Replace('\\', '/');
        var iconItems = CalculateItems(faviconFilesDir, faviconFilesUrl);

        await CreateIcons(file, iconItems, faviconFilesDir, cancellationToken);
        var siteManifestContent = GenerateSiteWebmanifestContent(iconItems, appName, appName, themeColor: faviconOption.ThemeColor, backgroundColor: faviconOption.BackgroundColor);
        _fileStorage.Write(Path.Combine(faviconFilesDir, "site.webmanifest"), siteManifestContent);
        return GenerateMetaHtml(iconItems, faviconOption.ThemeColor, faviconFilesUrl);
    }

    internal string GenerateMetaHtml(FaviconImageItem[] icons, string themeColor, string faviconFilesUrl)
    {
        var sb = new StringBuilder();

        var tabulation = '\t';

        foreach (var icon in icons)
        {
            var ext = Path.GetExtension(icon.FileName).TrimStart('.').ToLowerInvariant();
            sb.AppendLine($"""{tabulation}<link rel="{icon.Rel}" type="image/{ext}" sizes="{icon.Width}x{icon.Height}" href="{icon.FileUrl}">""");
        }
        var manifestUrl = UrlTool.Combine(faviconFilesUrl, "site.webmanifest");
        sb.AppendLine($"""{tabulation}<link rel="manifest" href="{manifestUrl}">""");
        sb.AppendLine($"""{tabulation}<meta name="msapplication-TileColor" content="{themeColor}">""");
        sb.AppendLine($"""{tabulation}<meta name="msapplication-TileImage" content="{UrlTool.Combine(faviconFilesUrl, "mstile-150x150.png")}">""");
        sb.AppendLine($"""{tabulation}<meta name="theme-color" content="{themeColor}">""");

        return sb.ToString().TrimStart();
    }

    internal FaviconImageItem[] CalculateItems(string path, string url)
    {
        path = path.TrimEnd('/');
        url = url.TrimEnd('/');
        return [
            new (){ Width = 16,     Height = 16,    Rel="icon", FileName = "favicon-16x16.png", FilePath = $"{path}/favicon-16x16.png", FileUrl=$"{url}/favicon-16x16.png" },
            new (){ Width = 32,     Height = 32,    Rel="icon", FileName = "favicon-32x32.png", FilePath = $"{path}/favicon-32x32.png", FileUrl=$"{url}/favicon-32x32.png" },
            new (){ Width = 48,     Height = 48,    Rel="icon", FileName = "favicon-48x48.png", FilePath = $"{path}/favicon-48x48.png", FileUrl=$"{url}/favicon-48x48.png" },
            new (){ Width = 64,     Height = 64,    Rel="icon", FileName = "favicon-64x64.png", FilePath = $"{path}/favicon-64x64.png", FileUrl=$"{url}/favicon-64x64.png" },
            new (){ Width = 128,    Height = 128,   Rel="icon", FileName = "favicon-128x128.png", FilePath = $"{path}/favicon-128x128.png", FileUrl=$"{url}/favicon-128x128.png" },
            new (){ Width = 180,    Height = 180,   Rel="apple-touch-icon", FileName = "apple-touch-icon.png", FilePath = $"{path}/apple-touch-icon.png", FileUrl=$"{url}/apple-touch-icon.png" },
            new (){ Width = 192,    Height = 192,   Rel="icon", FileName = "android-chrome-192x192.png", FilePath = $"{path}/android-chrome-192x192.png", FileUrl=$"{url}/android-chrome-192x192.png" },
            new (){ Width = 256,    Height = 256,   Rel="icon", FileName = "favicon-256x256.png", FilePath = $"{path}/favicon-256x256.png", FileUrl=$"{url}/favicon-256x256.png" },
            new (){ Width = 384,    Height = 384,   Rel="icon", FileName = "android-chrome-384x384.png", FilePath = $"{path}/android-chrome-384x384.png", FileUrl=$"{url}/android-chrome-384x384.png" },
            new (){ Width = 512,    Height = 512,   Rel="icon", FileName = "android-chrome-512x512.png", FilePath = $"{path}/android-chrome-512x512.png", FileUrl=$"{url}/android-chrome-512x512.png" },
            new (){ Width = 150,    Height = 150,   Rel="icon", FileName = "mstile-150x150.png", FilePath = $"{path}/mstile-150x150.png", FileUrl=$"{url}/mstile-150x150.png" },
        ];
    }

    internal async Task CreateIcons(FileDetail faviconSourceImage, FaviconImageItem[] items, string faviconFilesDir, CancellationToken cancellationToken)
    {
        if (!_fileStorage.DirectoryExists(faviconFilesDir))
            _fileStorage.CreateDirectory(faviconFilesDir);

        if (!_fileStorage.FileExists(faviconSourceImage.FilePhysicalPath))
            throw new FileNotFoundException("Favicon source image not found", faviconSourceImage.FilePhysicalPath);

        _fileStorage.Read(faviconSourceImage.FilePhysicalPath, out var sourceImageStream);

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cfg = new ImageConverConfig
            {
                Width = item.Width,
                Height = item.Height,
                Compression = ImagePreviewSizeConfig.EncoderCompression.Lossy,
                ResizeMode = ImagePreviewSizeConfig.CropScaleMode.Contain
            };

            using var ms = new MemoryStream();
            _imageProcessor.ProcessImage(sourceImageStream, ms, cfg);
            ms.Position = 0;
            await _fileStorage.WriteAsync(item.FilePath, ms, cancellationToken);
            sourceImageStream.Position = 0;
        }
    }

    internal static string GenerateSiteWebmanifestContent(FaviconImageItem[] icons, string appName, string appNameShort, string themeColor, string backgroundColor)
    {
        var manifest = new
        {
            name = appName,
            short_name = appNameShort,
            start_url = "/",
            display = "standalone",
            background_color = backgroundColor,
            theme_color = themeColor,
            icons = icons.Select(i => new
            {
                src = i.FileUrl,
                sizes = $"{i.Width}x{i.Height}",
                type = "image/" + Path.GetExtension(i.FilePath).TrimStart('.'),
                purpose = "any maskable"
            }).ToArray()
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(manifest, options);
    }
}

internal record FaviconImageItem
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required string FileName { get; init; }
    public required string FilePath { get; init; }
    public required string FileUrl { get; init; }
    public required string Rel { get; init; }
}
