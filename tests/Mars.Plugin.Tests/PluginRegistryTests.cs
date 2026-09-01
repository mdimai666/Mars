using FluentAssertions;
using Mars.Contracts.Dto.Files;
using Mars.Plugin.Contracts.Plugins;
using Mars.Plugin.Handlers;
using Mars.Plugin.Services;
using Mars.Storage.Services;

namespace Mars.Plugin.Tests;

public class PluginRegistryTests : IDisposable
{
    private readonly DirectoryInfo _root;
    private readonly DirectoryInfo _data;
    private readonly Mars.Server.Abstractions.Services.IFileStorage _fileStorage;

    public PluginRegistryTests()
    {
        _root = Directory.CreateTempSubdirectory("mars-plugin-registry-");
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
    public void MarkInstalled_ThenGet_ReturnsEntry()
    {
        var registry = new PluginRegistry(_fileStorage);
        var now = DateTimeOffset.UtcNow;

        registry.MarkInstalled("My.Plugin", PluginSource.NuGet, "1.2.3", now);

        var entry = registry.Get("My.Plugin");
        entry.Should().NotBeNull();
        entry!.Source.Should().Be(PluginSource.NuGet);
        entry.Version.Should().Be("1.2.3");
        entry.InstalledAtUtc.Should().Be(now);
        entry.Disabled.Should().BeFalse();
    }

    [Fact]
    public void SetDisabled_ThenIsDisabled_True()
    {
        var registry = new PluginRegistry(_fileStorage);
        registry.MarkInstalled("My.Plugin", PluginSource.Zip, "1.0.0", DateTimeOffset.UtcNow);

        registry.SetDisabled("My.Plugin", true);

        registry.IsDisabled("My.Plugin").Should().BeTrue();
    }

    [Fact]
    public void Remove_ThenGet_ReturnsNull()
    {
        var registry = new PluginRegistry(_fileStorage);
        registry.MarkInstalled("My.Plugin", PluginSource.Zip, "1.0.0", DateTimeOffset.UtcNow);

        registry.Remove("My.Plugin");

        registry.Get("My.Plugin").Should().BeNull();
    }

    [Fact]
    public void NewInstance_PersistsEntriesFromDisk()
    {
        var first = new PluginRegistry(_fileStorage);
        first.MarkInstalled("My.Plugin", PluginSource.NuGet, "2.0.0", DateTimeOffset.UtcNow);
        first.SetDisabled("My.Plugin", true);

        // второй экземпляр (как после рестарта) читает тот же файл
        var second = new PluginRegistry(_fileStorage);

        var entry = second.Get("My.Plugin");
        entry.Should().NotBeNull();
        entry!.Source.Should().Be(PluginSource.NuGet);
        entry.Version.Should().Be("2.0.0");
        second.IsDisabled("My.Plugin").Should().BeTrue();
    }
}
