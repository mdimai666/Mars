using System.Runtime.InteropServices;

namespace Mars.Host.Shared.Dto.Files;

public record FileHostingInfo
{
    public required Uri? Backend { get; init; }
    public required Uri PhysicalPath { get; init; }
    private string _requestPath = default!;
    public required string RequestPath { get => _requestPath; init => _requestPath = NormalizePathSlash(value) ?? ""; }


    string __normalizedAbsoluteRequestPath = default!;
    string NormalizedAbsoluteRequestPathAndSlash => __normalizedAbsoluteRequestPath ??=
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? NormalizePathSlash(PhysicalPath.AbsolutePath) + '/'
        : PhysicalPath.AbsolutePath + '/';

    const string ic_pdf = "/img/docs/pdf.png";
    const string ic_doc = "/img/docs/doc.png";
    const string ic_xls = "/img/docs/xls.png";
    const string ic_img = "/img/docs/img.png";
    const string ic_video = "/img/docs/video.png";
    const string ic_unknown = "/img/docs/unknown.png";

    HashSet<string> imgExtList = "jpg,png,jpeg,ico,gif,jfif,svg,webp".Split(',').ToHashSet(StringComparer.OrdinalIgnoreCase);
    HashSet<string> videoExtList = "mp4,avi,wmv,mov,flv,mkv,webm,mpeg".Split(',').ToHashSet(StringComparer.OrdinalIgnoreCase);

    public string PreviewIconUrl(string ext)
    {
        if (string.IsNullOrWhiteSpace(ext)) return ic_unknown;

        if (ext.StartsWith('.')) throw new ArgumentException("file extension cannot start with a dot");

        if (ExtIsImage(ext)) return ic_img;
        else if (ExtIsVideo(ext)) return ic_video;

        switch (ext)
        {
            case "pdf":
                return ic_pdf;
            case "doc":
            case "docx":
                return ic_doc;
            case "xls":
            case "xlsx":
                return ic_xls;
            default:
                return ic_unknown;
        }
    }

    public bool ExtIsImage(string ext)
    {
        if (string.IsNullOrEmpty(ext)) return false;
        if (ext.StartsWith('.')) throw new ArgumentException("file extension cannot start with a dot");

        return imgExtList.Contains(ext);
    }
    public bool ExtIsSvg(string ext)
    {
        if (string.IsNullOrEmpty(ext)) return false;
        if (ext.StartsWith('.')) throw new ArgumentException("file extension cannot start with a dot");

        return ext.Equals("svg", StringComparison.OrdinalIgnoreCase);
    }

    public bool ExtIsVideo(string ext)
    {
        if (string.IsNullOrEmpty(ext)) return false;
        if (ext.StartsWith('.')) throw new ArgumentException("file extension cannot start with a dot");

        return videoExtList.Contains(ext.ToLower());
    }

    public string FileAbsoluteUrlFromPath(string filePath)
    {
        if (Backend == null) return "";
        return Backend.AbsoluteUri + RequestPath + '/' + NormalizePathSlash(filePath);
    }

    public string FileRelativeUrlFromPath(string filePath)
    {
        if (Backend == null) return "";
        if (Backend.LocalPath == "/")
            return '/' + RequestPath + '/' + NormalizePathSlash(filePath);
        else
            return Backend.LocalPath + '/' + RequestPath + '/' + NormalizePathSlash(filePath);
    }

    public static string? NormalizePathSlash(string? fileUrl)
    {
        return fileUrl?.Replace('\\', '/').Trim('/');
    }

    public string? NormalizePathSlashes(string? fileUrl)
    {
        return fileUrl?.Replace('\\', '/').Trim('/');
    }

    public string GetExtension(string filename) => Path.GetExtension(filename)?.TrimStart('.').ToLower() ?? "";

    public string FileAbsolutePath(string path)
    {
        ArgumentNullException.ThrowIfNull(path, nameof(path));
        //ArgumentException.ThrowIfNull(path, nameof(path));
        //if (OperatingSystem.IsWindows() && Path.IsPathRooted(path)) throw new ArgumentException("path is root");

        if (path.Contains("../") || path.Contains("..\\")) throw new ArgumentException("path cannot contain relative part");

        return NormalizedAbsoluteRequestPathAndSlash + NormalizePathSlash(path)!.TrimStart('/');
    }

    /// <summary>
    /// contain trailing slash
    /// <para/>
    /// <example>C:/www/mars/wwwRoot/upload/</example>
    /// </summary>
    /// <returns></returns>
    public string AbsoluteUploadPath() => NormalizedAbsoluteRequestPathAndSlash;
}
