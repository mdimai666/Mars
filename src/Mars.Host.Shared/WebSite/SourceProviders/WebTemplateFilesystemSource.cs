using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Interfaces;
using Mars.Host.Shared.WebSite.Models;

namespace Mars.Host.Shared.WebSite.SourceProviders;

public class WebTemplateFilesystemSource : IWebTemplateSource
{
    public bool IsFileSystem => true;
    public string path { get; }
    readonly WebFilesReadFilesystemService fileService;


    public WebTemplateFilesystemSource(string path, WebFilesReadFilesystemService fileService)
    {
        this.path = path;
        this.fileService = fileService;
    }


    public IEnumerable<WebPartSource> ReadParts()
    {

        var files = fileService.ScanFiles(path);

        foreach (var file in files)
        {
            //string content = File.ReadAllText(file); //TODO: here bug

            using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs);
            using TextReader reader = sr;
            string content = reader.ReadToEnd();
            string filename = Path.GetFileName(file);

            string fileRelPath = Path.GetRelativePath(path, file);

            string fileAccessName = fileRelPath.Substring(0, fileRelPath.Length - 5).Replace('\\', '/');

            yield return new WebPartSource(content, fileAccessName, filename, file, fileRelPath);

        }
    }
}
