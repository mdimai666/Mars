using System.Reflection;
using Mars.Plugin;

namespace Test.Mars.Plugin;

public class MarsPluginTests
{
    [Fact]
    public void TestScanFolderForPlugins()
    {
        //string rootDir = TestContext.CurrentContext.TestDirectory;
        var rootDir = Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..", "..", "..");

        string pluginFolder = "SamplePluginsFolder";

        string path = Path.Combine(rootDir, pluginFolder);

        var plugins = ApplicationPluginExtensions.ScanFolderForPlugins(path);

        Assert.True(plugins.Count() == 2);



    }
}
