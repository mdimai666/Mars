using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Mars.Core.Features;

public class TextZip
{
    public static byte[] Zip(string uncompressed)
    {
        byte[] ret;
        using (var outputMemory = new MemoryStream())
        {
            using (var gz = new GZipStream(outputMemory, CompressionLevel.Optimal))
            {
                using (var sw = new StreamWriter(gz, Encoding.UTF8))
                {
                    sw.Write(uncompressed);
                }
            }
            ret = outputMemory.ToArray();
        }
        return ret;
    }

    public static string Unzip(byte[] compressed)
    {
        string? ret = null;
        using (var inputMemory = new MemoryStream(compressed))
        {
            using (var gz = new GZipStream(inputMemory, CompressionMode.Decompress))
            {
                using (var sr = new StreamReader(gz, Encoding.UTF8))
                {
                    ret = sr.ReadToEnd();
                }
            }
        }
        return ret;
    }

    public static string ZipToBase64(string uncompressed)
    {
        if (uncompressed is null) return null!;
        var bytes = Zip(uncompressed);
        return Convert.ToBase64String(bytes);
    }

    public static string UnzipFromBase64(string compressed)
    {
        if (compressed is null) return null!;
        var bytes = Convert.FromBase64String(compressed);
        return Unzip(bytes);
    }
}
