using Mars.Host.Shared.WebSite.Interfaces;

namespace Mars.Host.Shared.Services;

public class WebFilesReadFilesystemService : IWebFilesService
{

    public string[] ScanFiles(string path)
    {
        string[] ignoreList = { "bin", "obj", ".git", "node_modules" };

        //Directory.GetFileSystemEntries(path, "*.html", SearchOption.AllDirectories);

        var files = FindAllFiles(path, "*.hbs", ignoreList);

        return files;
    }

    static string[] FindAllFiles(string rootDir, string pattern, string[] ignoreList)
    {
        var pathsToSearch = new Queue<string>();
        var foundFiles = new List<string>();

        pathsToSearch.Enqueue(rootDir);

        while (pathsToSearch.Count > 0)
        {
            var dir = pathsToSearch.Dequeue();

            try
            {
                var files = Directory.GetFiles(dir, pattern);
                foreach (var file in files)
                {
                    foundFiles.Add(file);
                }

                foreach (var subDir in Directory.GetDirectories(dir))
                {
                    string name = Path.GetFileName(subDir);
                    if (ignoreList.Contains(name)) continue;
                    pathsToSearch.Enqueue(subDir);
                }

            }
            catch (Exception /* TODO: catch correct exception */)
            {
                // Swallow.  Gulp!
            }
        }

        return foundFiles.ToArray();
    }

}
