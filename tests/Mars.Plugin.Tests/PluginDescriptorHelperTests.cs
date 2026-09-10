using System.Text.Json;
using FluentAssertions;
using Mars.Plugin.Abstractions.Dto.Plugins;
using Mars.Plugin.Handlers;

namespace Mars.Plugin.Tests;

public class PluginDescriptorHelperTests
{
    [Theory]
    [InlineData("0.8.1-alpha.4", "0.8.1")]
    [InlineData("1.0.0", "1.0.0")]
    [InlineData("2.3", "2.3")]
    public void ParseVersionPrefix_StripsSuffix(string input, string expected)
    {
        PluginDescriptorHelper.ParseVersionPrefix(input).Should().Be(Version.Parse(expected));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-version")]
    public void ParseVersionPrefix_Invalid_ReturnsNull(string? input)
    {
        PluginDescriptorHelper.ParseVersionPrefix(input).Should().BeNull();
    }

    [Fact]
    public void TryRead_ValidJson_ReturnsDescriptor()
    {
        var dir = Directory.CreateTempSubdirectory();
        var descriptor = new PluginPackageDescriptor
        {
            PackageType = PluginPackageDescriptor.MarsPluginPackageType,
            PackageId = "com.example.plugin",
            Version = "1.0.0",
            EntryAssembly = "Plugin.dll",
            MarsVersion = "0.8.1-alpha.4",
        };
        var path = Path.Combine(dir.FullName, PluginPackageDescriptor.FileName);
        File.WriteAllText(path, JsonSerializer.Serialize(descriptor));

        var loaded = PluginDescriptorHelper.TryRead(path);

        loaded.Should().NotBeNull();
        loaded!.PackageId.Should().Be("com.example.plugin");
        loaded.EntryAssembly.Should().Be("Plugin.dll");
    }

    [Fact]
    public void TryRead_MissingFile_ReturnsNull()
    {
        PluginDescriptorHelper.TryRead(Path.Combine(Path.GetTempPath(), "definitely-missing.json")).Should().BeNull();
    }

    [Fact]
    public void TryRead_InvalidJson_ReturnsNull()
    {
        var dir = Directory.CreateTempSubdirectory();
        var path = Path.Combine(dir.FullName, PluginPackageDescriptor.FileName);
        File.WriteAllText(path, "{ not json");

        PluginDescriptorHelper.TryRead(path).Should().BeNull();
    }

    [Fact]
    public void Validate_MissingEntryAssembly_Throws()
    {
        var dir = Directory.CreateTempSubdirectory();
        var descriptor = new PluginPackageDescriptor
        {
            PackageType = PluginPackageDescriptor.MarsPluginPackageType,
            EntryAssembly = "Missing.dll",
        };

        var act = () => PluginDescriptorHelper.Validate(descriptor, dir.FullName);

        act.Should().Throw<Exception>().WithMessage("*Missing.dll*");
    }

    [Fact]
    public void Validate_WrongPackageType_Throws()
    {
        var dir = Directory.CreateTempSubdirectory();
        File.WriteAllText(Path.Combine(dir.FullName, "Plugin.dll"), "x");
        var descriptor = new PluginPackageDescriptor
        {
            PackageType = "SomethingElse",
            EntryAssembly = "Plugin.dll",
        };

        var act = () => PluginDescriptorHelper.Validate(descriptor, dir.FullName);

        act.Should().Throw<Exception>().WithMessage("*SomethingElse*");
    }

    [Fact]
    public void Validate_ValidLayout_Succeeds()
    {
        var dir = Directory.CreateTempSubdirectory();
        File.WriteAllText(Path.Combine(dir.FullName, "Plugin.dll"), "x");
        var descriptor = new PluginPackageDescriptor
        {
            PackageType = PluginPackageDescriptor.MarsPluginPackageType,
            EntryAssembly = "Plugin.dll",
            MarsVersion = "0.0.1", // ниже любой версии хоста
        };

        var act = () => PluginDescriptorHelper.Validate(descriptor, dir.FullName);

        act.Should().NotThrow();
    }
}
