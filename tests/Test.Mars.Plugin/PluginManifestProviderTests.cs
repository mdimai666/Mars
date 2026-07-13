using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Mars.Plugin.PluginProvider.Dto;
using Mars.Plugin.PluginProvider.Providers;

namespace Test.Mars.Plugin;

public class PluginManifestProviderTests
{
    string _testFilesDir;

    public PluginManifestProviderTests()
    {
        _testFilesDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "SampleWebStaticAssetEndpointsFiles"));
    }

    [Theory]
    [InlineData([false])]
    [InlineData([true])]
    public void FilterFiles_FilterSharedFildes_OnlyThePluginFilesShouldRemain(bool releaseMode)
    {
        _ = nameof(PluginManifestProvider.FilterFiles);
        // Arrange
        string marsDevAdminEndpointsFile, pluginStaticwebassetsEndpointsFile;

        if (releaseMode)
        {
            marsDevAdminEndpointsFile = Path.Combine(_testFilesDir, "Release_AppAdmin.staticwebassets.endpoints.json");
            pluginStaticwebassetsEndpointsFile = Path.Combine(_testFilesDir, "Release_Mars.PlayAudioNodePlugin.staticwebassets.endpoints.json");
        }
        else
        {
            marsDevAdminEndpointsFile = Path.Combine(_testFilesDir, "Debug_AppAdmin.staticwebassets.endpoints.json");
            pluginStaticwebassetsEndpointsFile = Path.Combine(_testFilesDir, "Debug_Mars.PlayAudioNodePlugin.staticwebassets.endpoints.json");
        }

        var pluginStaticwebassetsEndpoints = JsonSerializer.Deserialize<StaticwebassetsEndpointsManifestJson>(File.ReadAllText(pluginStaticwebassetsEndpointsFile))!;
        var marsDevAdminEndpoints = JsonSerializer.Deserialize<StaticwebassetsEndpointsManifestJson>(File.ReadAllText(marsDevAdminEndpointsFile))!;

        var coreReg = new Regex(@"_framework/Mars\.Core\.(\w+)\.wasm");
        var purePluginFilesCountExpect = 25;

        // Act
        var files = PluginManifestProvider.FilterFiles(marsDevAdminEndpoints, pluginStaticwebassetsEndpoints);

        // Assert
        if (files.FirstOrDefault(s => coreReg.IsMatch(s.AssetFile)) != null)
            Assert.Fail("core files exist");
        if (files.FirstOrDefault(s => s.AssetFile.StartsWith("_framework/icudt_")) != null)
            Assert.Fail("icdu files exist");
        if (files.FirstOrDefault(s => s.AssetFile.StartsWith("_framework/Microsoft.DotNet.HotReload")) != null)
            Assert.Fail("HotReload files exist");
        if (files.FirstOrDefault(s => s.AssetFile.EndsWith(".pdb")) != null)
            Assert.Fail("pdb files exist");
        var nonCompressedFilesOnly = files.Where(f => !f.AssetFile.EndsWith(".gz") && !f.AssetFile.EndsWith(".br")).ToList();
        nonCompressedFilesOnly.Count.Should().Be(purePluginFilesCountExpect);
    }
}
