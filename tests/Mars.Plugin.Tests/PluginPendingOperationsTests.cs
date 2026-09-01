using System.Text.Json;
using FluentAssertions;
using Mars.Contracts.Dto.Files;
using Mars.Plugin.Abstractions.Dto.Plugins;
using Mars.Plugin.Contracts.Plugins;
using Mars.Plugin.Handlers;
using Mars.Plugin.Services;
using Mars.Storage.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mars.Plugin.Tests;

public class PluginPendingOperationsTests : IDisposable
{
    private readonly DirectoryInfo _root;
    private readonly DirectoryInfo _data;
    private readonly Mars.Server.Abstractions.Services.IFileStorage _fileStorage;

    public PluginPendingOperationsTests()
    {
        _root = Directory.CreateTempSubdirectory("mars-plugin-pending-");
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

    string PluginDirPath(string packageId)
        => Path.Combine(_data.FullName, PluginManager.PluginsDefaultPath, packageId);

    DirectoryInfo CreatePluginFolder(string packageId, params string[] fileNames)
    {
        var dir = new DirectoryInfo(PluginDirPath(packageId));
        dir.Create();
        foreach (var name in fileNames)
            File.WriteAllText(Path.Combine(dir.FullName, name), "dummy");
        return dir;
    }

    DirectoryInfo CreateDescriptorFolder(string packageId)
    {
        var dir = CreatePluginFolder(packageId, $"{packageId}.dll");
        var descriptor = new PluginPackageDescriptor
        {
            PackageType = PluginPackageDescriptor.MarsPluginPackageType,
            PackageId = packageId,
            Version = "1.0.0",
            EntryAssembly = $"{packageId}.dll",
        };
        File.WriteAllText(Path.Combine(dir.FullName, PluginPackageDescriptor.FileName), JsonSerializer.Serialize(descriptor));
        return dir;
    }

    [Fact]
    public void ApplyPendingOperations_PendingDelete_DeletesFolderAndRemovesEntry()
    {
        const string packageId = "My.Plugin";
        CreatePluginFolder(packageId, $"{packageId}.dll");
        var registry = new PluginRegistry(_fileStorage);
        registry.MarkInstalled(packageId, PluginSource.Zip, "1.0.0", DateTimeOffset.UtcNow);
        registry.MarkPendingDelete(packageId);

        var manager = new PluginManager(NullLogger<PluginManager>.Instance, _fileStorage);
        manager.ApplyPendingOperations();

        Directory.Exists(PluginDirPath(packageId)).Should().BeFalse();
        manager.Registry.Get(packageId).Should().BeNull();
    }

    [Fact]
    public void ApplyPendingOperations_PendingDelete_LockedFolder_KeepsMark()
    {
        if (!OperatingSystem.IsWindows()) return; // read-only файл блокирует удаление папки только на Windows

        const string packageId = "Locked.Plugin";
        CreatePluginFolder(packageId, $"{packageId}.dll");
        var lockedFile = Path.Combine(PluginDirPath(packageId), $"{packageId}.dll");
        File.SetAttributes(lockedFile, FileAttributes.ReadOnly);

        try
        {
            var registry = new PluginRegistry(_fileStorage);
            registry.MarkInstalled(packageId, PluginSource.Zip, "1.0.0", DateTimeOffset.UtcNow);
            registry.MarkPendingDelete(packageId);

            var manager = new PluginManager(NullLogger<PluginManager>.Instance, _fileStorage);
            manager.ApplyPendingOperations();

            Directory.Exists(PluginDirPath(packageId)).Should().BeTrue();
            manager.Registry.Get(packageId)!.PendingDelete.Should().BeTrue();
        }
        finally
        {
            File.SetAttributes(lockedFile, FileAttributes.Normal);
        }
    }

    [Fact]
    public void ApplyPendingOperations_PendingStagingDir_ReplacesFolderAndClearsMark()
    {
        const string packageId = "My.Plugin";
        CreatePluginFolder(packageId, "old.txt");
        var stagingName = $"_pending_{packageId}_x";
        CreatePluginFolder(stagingName, "new.txt");

        var registry = new PluginRegistry(_fileStorage);
        registry.MarkInstalled(packageId, PluginSource.NuGet, "2.0.0", DateTimeOffset.UtcNow,
            pendingStagingDir: Path.Combine(PluginManager.PluginsDefaultPath, stagingName));

        var manager = new PluginManager(NullLogger<PluginManager>.Instance, _fileStorage);
        manager.ApplyPendingOperations();

        Directory.Exists(PluginDirPath(stagingName)).Should().BeFalse();
        File.Exists(Path.Combine(PluginDirPath(packageId), "new.txt")).Should().BeTrue();
        File.Exists(Path.Combine(PluginDirPath(packageId), "old.txt")).Should().BeFalse();

        var entry = manager.Registry.Get(packageId)!;
        entry.PendingStagingDir.Should().BeNull();
        entry.Version.Should().Be("2.0.0");
    }

    [Fact]
    public void ApplyPendingOperations_StagingMissing_DropsMark()
    {
        const string packageId = "Ghost.Plugin";
        var registry = new PluginRegistry(_fileStorage);
        registry.MarkInstalled(packageId, PluginSource.NuGet, "2.0.0", DateTimeOffset.UtcNow,
            pendingStagingDir: Path.Combine(PluginManager.PluginsDefaultPath, "_pending_ghost"));

        var manager = new PluginManager(NullLogger<PluginManager>.Instance, _fileStorage);
        manager.ApplyPendingOperations();

        manager.Registry.Get(packageId)!.PendingStagingDir.Should().BeNull();
    }

    [Fact]
    public void ReadPluginsFromDirectory_DisabledOrPendingDelete_AssemblyNotLoaded()
    {
        CreateDescriptorFolder("Active.Plugin");
        CreateDescriptorFolder("Disabled.Plugin");
        CreateDescriptorFolder("Deleted.Plugin");

        var registry = new PluginRegistry(_fileStorage);
        registry.MarkInstalled("Active.Plugin", PluginSource.Zip, "1.0.0", DateTimeOffset.UtcNow);
        registry.MarkInstalled("Disabled.Plugin", PluginSource.Zip, "1.0.0", DateTimeOffset.UtcNow);
        registry.MarkInstalled("Deleted.Plugin", PluginSource.Zip, "1.0.0", DateTimeOffset.UtcNow);
        registry.SetDisabled("Disabled.Plugin", true);
        registry.MarkPendingDelete("Deleted.Plugin");

        var manager = new PluginManager(NullLogger<PluginManager>.Instance, _fileStorage);

        var configs = manager.ReadPluginsFromDirectory(_fileStorage, PluginManager.PluginsDefaultPath, NullLogger.Instance).ToList();

        configs.Should().HaveCount(1);
        configs[0].AssemblyPath.Should().EndWith("Active.Plugin.dll");
    }
}
