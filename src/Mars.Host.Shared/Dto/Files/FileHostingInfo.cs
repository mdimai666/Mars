using System.Runtime.InteropServices;

namespace Mars.Host.Shared.Dto.Files;

public record FileHostingInfo
{
    public required Uri Backend { get; init; }
    public required Uri wwwRoot { get; init; }
    private string _uploadSubPath = default!;
    public required string UploadSubPath { get => _uploadSubPath; init => _uploadSubPath = NormalizePathSlash(value) ?? ""; }


    string __normalizedAbsoluteUpload = default!;
    string NormalizedAbsoluteUploadAndSlash => __normalizedAbsoluteUpload ??=
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? NormalizePathSlash(wwwRoot.AbsolutePath) + '/' + _uploadSubPath + '/'
        : wwwRoot.AbsolutePath + '/' + _uploadSubPath + '/';

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
        return Backend.AbsoluteUri + UploadSubPath + '/' + NormalizePathSlash(filePath);
    }

    public string FileRelativeUrlFromPath(string filePath)
    {
        if (Backend.LocalPath == "/")
            return '/' + UploadSubPath + '/' + NormalizePathSlash(filePath);
        else
            return Backend.LocalPath + '/' + UploadSubPath + '/' + NormalizePathSlash(filePath);
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

        return NormalizedAbsoluteUploadAndSlash + NormalizePathSlash(path)!.TrimStart('/');
    }

    /// <summary>
    /// contain trailing slash
    /// <para/>
    /// <example>C:/www/mars/wwwRoot/upload/</example>
    /// </summary>
    /// <returns></returns>
    public string AbsoluteUploadPath() => NormalizedAbsoluteUploadAndSlash;
}
