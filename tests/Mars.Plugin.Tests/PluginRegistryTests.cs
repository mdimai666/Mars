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

    [Fact]
    public void MarkPendingDelete_FlagTrue_AndPersists()
    {
        var registry = new PluginRegistry(_fileStorage);
        registry.MarkInstalled("My.Plugin", PluginSource.Zip, "1.0.0", DateTimeOffset.UtcNow);

        registry.MarkPendingDelete("My.Plugin");

        registry.Get("My.Plugin")!.PendingDelete.Should().BeTrue();

        var reloaded = new PluginRegistry(_fileStorage);
        reloaded.Get("My.Plugin")!.PendingDelete.Should().BeTrue();
    }

    [Fact]
    public void MarkInstalled_WithPendingStagingDir_PersistsIt()
    {
        var registry = new PluginRegistry(_fileStorage);
        var staging = Path.Combine(PluginManager.PluginsDefaultPath, "_pending_My.Plugin_x");

        registry.MarkInstalled("My.Plugin", PluginSource.NuGet, "2.0.0", DateTimeOffset.UtcNow, pendingStagingDir: staging);

        var reloaded = new PluginRegistry(_fileStorage);
        var entry = reloaded.Get("My.Plugin")!;
        entry.PendingStagingDir.Should().Be(staging);
        entry.PendingDelete.Should().BeFalse();
    }

    [Fact]
    public void MarkInstalled_OverPendingDelete_ResetsMarks_KeepsDisabled()
    {
        var registry = new PluginRegistry(_fileStorage);
        registry.MarkInstalled("My.Plugin", PluginSource.Zip, "1.0.0", DateTimeOffset.UtcNow);
        registry.SetDisabled("My.Plugin", true);
        registry.MarkPendingDelete("My.Plugin");

        // переустановка до рестарта отменяет удаление
        registry.MarkInstalled("My.Plugin", PluginSource.NuGet, "2.0.0", DateTimeOffset.UtcNow);

        var entry = registry.Get("My.Plugin")!;
        entry.PendingDelete.Should().BeFalse();
        entry.PendingStagingDir.Should().BeNull();
        entry.Disabled.Should().BeTrue();
        entry.Version.Should().Be("2.0.0");
    }

    [Fact]
    public void ClearPendingMarks_ClearsBothFlags()
    {
        var registry = new PluginRegistry(_fileStorage);
        registry.MarkInstalled("My.Plugin", PluginSource.Zip, "1.0.0", DateTimeOffset.UtcNow,
            pendingStagingDir: Path.Combine(PluginManager.PluginsDefaultPath, "_pending_My.Plugin_x"));
        registry.MarkPendingDelete("My.Plugin");

        registry.ClearPendingMarks("My.Plugin");

        var entry = registry.Get("My.Plugin")!;
        entry.PendingDelete.Should().BeFalse();
        entry.PendingStagingDir.Should().BeNull();
    }

    [Fact]
    public void OldRegistryFile_WithoutNewFields_ReadsWithDefaults()
    {
        var path = Path.Combine(_data.FullName, PluginManager.PluginsDefaultPath, ".registry.json");
        File.WriteAllText(path, """
            {
              "My.Plugin": {
                "Source": 2,
                "Version": "1.0.0",
                "InstalledAtUtc": "2026-09-01T00:00:00+00:00",
                "Disabled": false
              }
            }
            """);

        var registry = new PluginRegistry(_fileStorage);

        var entry = registry.Get("My.Plugin");
        entry.Should().NotBeNull();
        entry!.Source.Should().Be(PluginSource.Zip);
        entry.PendingDelete.Should().BeFalse();
        entry.PendingStagingDir.Should().BeNull();
    }
}
