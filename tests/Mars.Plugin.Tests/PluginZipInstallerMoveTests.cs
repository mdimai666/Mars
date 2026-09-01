using FluentAssertions;
using Mars.Contracts.Dto.Files;
using Mars.Core.Exceptions;
using Mars.Plugin.Handlers;
using Mars.Storage.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mars.Plugin.Tests;

/// <summary>
/// Регрессия «Access to the path ... is denied» при установке из zip: свежезапакованные
/// сборки удерживает внешний процесс (антивирус), и Directory.Move обязан переждать лок ретраями.
/// </summary>
public class PluginZipInstallerMoveTests : IDisposable
{
    private readonly DirectoryInfo _root;
    private readonly FileStorage _storage;
    private readonly PluginZipInstaller _installer;

    public PluginZipInstallerMoveTests()
    {
        _root = Directory.CreateTempSubdirectory("mars-zip-installer-");
        _storage = new FileStorage(Microsoft.Extensions.Options.Options.Create(new FileHostingInfo
        {
            Backend = null,
            PhysicalPath = new Uri(_root.FullName, UriKind.Absolute),
            RequestPath = ""
        }));
        _installer = new PluginZipInstaller(_storage, NullLogger<PluginZipInstaller>.Instance, new PluginRegistry(_storage));
    }

    public void Dispose()
    {
        try { _root.Delete(recursive: true); }
        catch { /* временная папка */ }
    }

    (string staging, string final, string lockedFile) CreateStagingWithFile()
    {
        var staging = Path.Combine("plugins", $"_upload_{Guid.NewGuid():N}");
        var final = Path.Combine("plugins", "mdimai666.Test.Plugin");

        _storage.CreateDirectory(Path.Combine(staging, "libs"));
        _storage.Write(Path.Combine(staging, "mars-plugin.json"), "{}");

        var lockedFile = Path.Combine(_root.FullName, staging.Replace('/', Path.DirectorySeparatorChar), "libs", "Test.Plugin.dll");
        File.WriteAllBytes(lockedFile, [1, 2, 3]);

        return (staging, final, lockedFile);
    }

    [Fact]
    public async Task Move_LockReleasedDuringRetries_MoveSucceeds()
    {
        //Arrange
        var (staging, final, lockedFile) = CreateStagingWithFile();
        var hold = new FileStream(lockedFile, FileMode.Open, FileAccess.Read, FileShare.None);
        var release = Task.Run(async () =>
        {
            await Task.Delay(700);
            await hold.DisposeAsync();
        });

        //Act
        await _installer.MoveInstalledPluginAsync(staging, final, CancellationToken.None, attempts: 8, initialDelay: TimeSpan.FromMilliseconds(200));
        await release;

        //Assert
        Directory.Exists(Path.Combine(_root.FullName, final.Replace('/', Path.DirectorySeparatorChar))).Should().BeTrue();
        Directory.Exists(Path.Combine(_root.FullName, staging.Replace('/', Path.DirectorySeparatorChar))).Should().BeFalse();
    }

    [Fact]
    public async Task Move_PersistentLock_ThrowsUserActionException()
    {
        //Arrange
        var (staging, final, lockedFile) = CreateStagingWithFile();
        await using var hold = new FileStream(lockedFile, FileMode.Open, FileAccess.Read, FileShare.None);

        //Act
        var act = () => _installer.MoveInstalledPluginAsync(staging, final, CancellationToken.None, attempts: 3, initialDelay: TimeSpan.FromMilliseconds(50));

        //Assert
        await act.Should().ThrowAsync<UserActionException>();
        Directory.Exists(Path.Combine(_root.FullName, final.Replace('/', Path.DirectorySeparatorChar))).Should().BeFalse();
        Directory.Exists(Path.Combine(_root.FullName, staging.Replace('/', Path.DirectorySeparatorChar))).Should().BeTrue();
    }
}
