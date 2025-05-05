namespace Mars.Plugin.Integration.Tests.Tests;

public class PluginFilesTests
{
    public static string PluginDllPath()
    {
        var marsDir = Directory.GetCurrentDirectory().Split(@"\Mars\", 2)[0] + @"\Mars\src\";

        var pluginIsolatedDir = @"Plugin\PluginExample\bin\Debug\net9.0\plugin-subfolder\net9.0";
        var pluginDll = "PluginExample.dll";

        return Path.Combine(marsDir, pluginIsolatedDir, pluginDll);
    }

    [Fact]
    public void ScanFiles_ForFuture_Unused()
    {
        var dir = PluginDllPath();

        var fileExist = File.Exists(dir);
    }
}
