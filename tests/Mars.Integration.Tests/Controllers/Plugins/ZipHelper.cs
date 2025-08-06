using System.IO.Compression;

namespace Mars.Integration.Tests.Controllers.Plugins;

public static class ZipHelper
{
    public static Stream ZipFiles(Dictionary<string, byte[]> files)
    {
        var outputStream = new MemoryStream();

        using (var archive = new ZipArchive(outputStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in files)
            {
                var entry = archive.CreateEntry(file.Key, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                entryStream.Write(file.Value, 0, file.Value.Length);
            }
        }

        outputStream.Position = 0;
        return outputStream;
    }
}
