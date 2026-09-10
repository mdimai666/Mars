using System.Text.Json;
using FluentAssertions;
using Mars.Contracts.Dto.Files;
using Mars.Plugin.Abstractions.Dto.Plugins;
using Mars.Plugin.Services;
using Mars.Storage.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mars.Plugin.Tests;

public class PluginManagerDetectionTests : IDisposable
{
    private readonly DirectoryInfo _root;
    private readonly DirectoryInfo _data;
    private readonly Mars.Server.Abstractions.Services.IFileStorage _fileStorage;

    public PluginManagerDetectionTests()
    {
        _root = Directory.CreateTempSubdirectory("mars-plugin-detect-");
        _data = _root.CreateSubdirectory("data");
        _data.CreateSubdirectory(PluginManager.PluginsDefaultPath);

        var hostingInfo = Microsoft.Extensions.Options.Options.Create(new FileHostingInfo
        {
            Backend = null,
            PhysicalPath = new Uri(_data.FullName, UriKind.Absolute),
            RequestPath = ""
        });
        _fileStorage = new FileStorage(hostingInfo);
    }

    public void Dispose()
    {
        try { _root.Delete(recursive: true); }
        catch { /* временная папка */ }
    }

    [Fact]
    public void ReadPluginsFromDirectory_DescriptorLayout_ReturnsConfig()
    {
        // Arrange: папка плагина с дескриптором и входной сборкой
        const string packageId = "My.Plugin";
        var pluginFolder = new DirectoryInfo(Path.Combine(_data.FullName, PluginManager.PluginsDefaultPath, packageId));
        pluginFolder.Create();
        var descriptor = new PluginPackageDescriptor
        {
            PackageType = PluginPackageDescriptor.MarsPluginPackageType,
            PackageId = packageId,
            Version = "1.0.0",
            EntryAssembly = $"{packageId}.dll",
        };
        File.WriteAllText(Path.Combine(pluginFolder.FullName, PluginPackageDescriptor.FileName), JsonSerializer.Serialize(descriptor));
        File.WriteAllText(Path.Combine(pluginFolder.FullName, $"{packageId}.dll"), "dummy");

        var manager = new PluginManager(NullLogger<PluginManager>.Instance, _fileStorage);

        // Act
        var configs = manager.ReadPluginsFromDirectory(_fileStorage, PluginManager.PluginsDefaultPath, NullLogger.Instance).ToList();

        // Assert
        configs.Should().HaveCount(1);
        configs[0].AssemblyPath.Should().EndWith($"{packageId}.dll");
        configs[0].ContentRootPath.Should().Be(pluginFolder.FullName);
    }

    [Fact]
    public void ReadPluginsFromDirectory_MissingEntryDll_Skipped()
    {
        // дескриптор есть, а входная сборка отсутствует — папка пропускается
        var pluginFolder = new DirectoryInfo(Path.Combine(_data.FullName, PluginManager.PluginsDefaultPath, "Broken.Plugin"));
        pluginFolder.Create();
        var descriptor = new PluginPackageDescriptor
        {
            PackageType = PluginPackageDescriptor.MarsPluginPackageType,
            PackageId = "Broken.Plugin",
            EntryAssembly = "Absent.dll",
        };
        File.WriteAllText(Path.Combine(pluginFolder.FullName, PluginPackageDescriptor.FileName), JsonSerializer.Serialize(descriptor));

        var manager = new PluginManager(NullLogger<PluginManager>.Instance, _fileStorage);

        var configs = manager.ReadPluginsFromDirectory(_fileStorage, PluginManager.PluginsDefaultPath, NullLogger.Instance).ToList();

        configs.Should().BeEmpty();
    }

    [Fact]
    public void ReadPluginsFromDirectory_UnderscorePrefix_Skipped()
    {
        var pluginFolder = new DirectoryInfo(Path.Combine(_data.FullName, PluginManager.PluginsDefaultPath, "_hidden"));
        pluginFolder.Create();
        File.WriteAllText(Path.Combine(pluginFolder.FullName, "_hidden.dll"), "dummy");

        var manager = new PluginManager(NullLogger<PluginManager>.Instance, _fileStorage);

        var configs = manager.ReadPluginsFromDirectory(_fileStorage, PluginManager.PluginsDefaultPath, NullLogger.Instance).ToList();

        configs.Should().BeEmpty();
    }
}
