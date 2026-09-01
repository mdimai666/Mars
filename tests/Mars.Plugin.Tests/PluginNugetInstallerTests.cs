using System.IO.Compression;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Mars.Contracts.Dto.Files;
using Mars.Plugin.Abstractions.Dto.Plugins;
using Mars.Plugin.Handlers;
using Mars.Plugin.Services;
using Mars.Storage.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mars.Plugin.Tests;

public class PluginNugetInstallerTests : IDisposable
{
    private readonly DirectoryInfo _root;
    private readonly DirectoryInfo _feed;
    private readonly DirectoryInfo _data;
    private readonly Mars.Server.Abstractions.Services.IFileStorage _fileStorage;

    public PluginNugetInstallerTests()
    {
        _root = Directory.CreateTempSubdirectory("mars-nuget-installer-");
        _feed = _root.CreateSubdirectory("feed");
        _data = _root.CreateSubdirectory("data");

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
    public async Task InstallAsync_FromLocalFeed_LaysOutPluginFolder()
    {
        // Arrange: локальный фид с одним плагином без зависимостей
        const string packageId = "Com.Example.Plugin";
        const string version = "1.0.0";
        BuildPluginNupkg(packageId, version, DescriptorJson(packageId, version));

        var installer = new PluginNugetInstaller(_fileStorage, NullLogger.Instance);

        // Act
        var result = await installer.InstallAsync(packageId, null, [_feed.FullName], CancellationToken.None);

        // Assert
        result.PackageId.Should().Be(packageId);
        result.Version.Should().Be(version);

        var pluginDir = Path.Combine(_data.FullName, PluginManager.PluginsDefaultPath, packageId);
        Directory.Exists(pluginDir).Should().BeTrue();
        File.Exists(Path.Combine(pluginDir, PluginPackageDescriptor.FileName)).Should().BeTrue();
        File.Exists(Path.Combine(pluginDir, $"{packageId}.dll")).Should().BeTrue();
        File.Exists(Path.Combine(pluginDir, "wwwroot", "icon.png")).Should().BeTrue();
    }

    [Fact]
    public async Task InstallAsync_NotMarsPlugin_Throws()
    {
        // пакет без дескриптора — не плагин Марса
        const string packageId = "Com.Example.NotAPlugin";
        BuildPlainNupkg(packageId, "1.0.0");

        var installer = new PluginNugetInstaller(_fileStorage, NullLogger.Instance);

        var act = () => installer.InstallAsync(packageId, null, [_feed.FullName], CancellationToken.None);

        await act.Should().ThrowAsync<Exception>().WithMessage("*not a Mars plugin*");
    }

    [Fact]
    public async Task InstallAsync_ResolvesDependencies_AndFiltersMarsAssemblies()
    {
        // Arrange: плагин зависит от двух пакетов; сборка одного из них уже есть в замыкании Марса
        const string packageId = "Com.Example.Plugin";
        const string version = "1.0.0";
        BuildDepNupkg("Blocked.Dep", "1.0.0");
        BuildDepNupkg("Needed.Dep", "1.0.0");

        var marsDepsJson = Path.Combine(_root.FullName, "Mars.deps.json");
        File.WriteAllText(marsDepsJson, BuildMarsDepsJson("Blocked.Dep"));

        var dependenciesXml = """
            <dependencies>
              <group targetFramework="net10.0">
                <dependency id="Blocked.Dep" version="[1.0.0, )" />
                <dependency id="Needed.Dep" version="[1.0.0, )" />
              </group>
            </dependencies>
            """;
        BuildPluginNupkg(packageId, version, DescriptorJson(packageId, version), dependenciesXml);

        var installer = new PluginNugetInstaller(_fileStorage, NullLogger.Instance, null, marsDepsJson);

        // Act
        await installer.InstallAsync(packageId, null, [_feed.FullName], CancellationToken.None);

        // Assert
        var pluginDir = Path.Combine(_data.FullName, PluginManager.PluginsDefaultPath, packageId);
        File.Exists(Path.Combine(pluginDir, "Needed.Dep.dll")).Should().BeTrue();
        File.Exists(Path.Combine(pluginDir, "Blocked.Dep.dll")).Should().BeFalse("сборка уже входит в замыкание Марса");
    }

    static string DescriptorJson(string packageId, string version) => JsonSerializer.Serialize(new PluginPackageDescriptor
    {
        PackageType = PluginPackageDescriptor.MarsPluginPackageType,
        PackageId = packageId,
        Version = version,
        EntryAssembly = $"{packageId}.dll",
        MarsVersion = "0.0.1",
    });

    void BuildPluginNupkg(string packageId, string version, string descriptorJson, string dependenciesXml = "")
    {
        using var zip = OpenNupkg(packageId, version, dependenciesXml);
        AddEntry(zip, $"lib/net10.0/{packageId}.dll", "dll");
        AddEntry(zip, "mars/" + PluginPackageDescriptor.FileName, descriptorJson);
        AddEntry(zip, "mars/front/icon.png", "png");
    }

    void BuildDepNupkg(string packageId, string version)
    {
        using var zip = OpenNupkg(packageId, version);
        AddEntry(zip, $"lib/net10.0/{packageId}.dll", "dep-dll");
    }

    void BuildPlainNupkg(string packageId, string version)
    {
        using var zip = OpenNupkg(packageId, version);
        AddEntry(zip, $"lib/net10.0/{packageId}.dll", "dll");
    }

    ZipArchive OpenNupkg(string packageId, string version, string dependenciesXml = "")
    {
        var nupkgPath = Path.Combine(_feed.FullName, $"{packageId}.{version}.nupkg");
        var zip = ZipFile.Open(nupkgPath, ZipArchiveMode.Create);
        AddEntry(zip, $"{packageId}.nuspec", BuildNuspec(packageId, version, dependenciesXml));
        return zip;
    }

    static string BuildNuspec(string packageId, string version, string dependenciesXml = "") => $"""
        <?xml version="1.0" encoding="utf-8"?>
        <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
          <metadata>
            <id>{packageId}</id>
            <version>{version}</version>
            <authors>test</authors>
            <description>test plugin</description>
            {dependenciesXml}
          </metadata>
        </package>
        """;

    /// <summary>deps.json с одной «марсовой» сборкой для проверки фильтрации.</summary>
    static string BuildMarsDepsJson(string blockedAssemblySimple) => $$"""
        {
          "runtimeTarget": { "name": ".NETCoreApp,Version=v10.0", "signature": "" },
          "compilationOptions": {},
          "targets": {
            ".NETCoreApp,Version=v10.0": {
              "{{blockedAssemblySimple}}/1.0.0": {
                "dependencies": {},
                "runtime": { "lib/net10.0/{{blockedAssemblySimple}}.dll": { "assemblyVersion": "1.0.0.0", "fileVersion": "1.0.0.0" } },
                "resources": {}
              }
            }
          },
          "libraries": {}
        }
        """;

    static void AddEntry(ZipArchive zip, string entryName, string content)
    {
        var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }
}
