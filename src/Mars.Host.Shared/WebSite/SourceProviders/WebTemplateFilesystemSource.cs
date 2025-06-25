using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Interfaces;
using Mars.Host.Shared.WebSite.Models;

namespace Mars.Host.Shared.WebSite.SourceProviders;

public class WebTemplateFilesystemSource : IWebTemplateSource
{
    public bool IsFileSystem => true;
    public string path { get; }
    readonly WebFilesReadFilesystemService _fileService;

    public WebTemplateFilesystemSource(string path, WebFilesReadFilesystemService fileService)
    {
        this.path = path;
        _fileService = fileService;
    }

    public IEnumerable<WebPartSource> ReadParts()
    {
        var files = _fileService.ScanFiles(path);

        if (!files.Any()) throw new Exception("WebSiteTemplate: any files not found");

        foreach (var file in files)
        {
            using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs);
            using TextReader reader = sr;
            string content = reader.ReadToEnd();
            string filename = System.IO.Path.GetFileName(file);

            string fileRelPath = System.IO.Path.GetRelativePath(path, file);

            string fileAccessName = Path.Join(Path.GetDirectoryName(fileRelPath), Path.GetFileNameWithoutExtension(file)).Replace('\\', '/');

            yield return new WebPartSource(content, fileAccessName, filename, file, fileRelPath);

        }
    }
}
