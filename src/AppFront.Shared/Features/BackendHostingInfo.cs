using System.Web;
using Mars.Core.Extensions;

namespace AppFront.Shared.Features;

public class BackendHostingInfo
{
    public required Uri Backend { get; init; }

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

    public bool UrlIsImage(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        var ext = GetExtension(url);
        return ExtIsImage(ext);
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

    public static string? NormalizePathSlash(string? fileUrl)
    {
        return fileUrl?.Replace('\\', '/').Trim('/');
    }

    public string? NormalizePathSlashes(string? fileUrl)
    {
        return fileUrl?.Replace('\\', '/').Trim('/');
    }

    public string GetExtension(string filename) => Path.GetExtension(filename)?.TrimStart('.').ToLower() ?? "";

    /// <summary>
    /// Объединяем разные пути нормализуе слеши и убирая лишние слеши
    /// </summary>
    /// <param name="paths"></param>
    /// <returns></returns>
    public string NormalizedPathJoin(params string?[] paths)
        => HttpUtility.UrlEncode(paths.TrimNullOrEmpty().Select(s => NormalizePathSlashes(s).TrimEnd('/')).JoinStr("/"));
}
