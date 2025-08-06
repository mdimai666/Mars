using System.Xml;
using FluentAssertions;
using Mars.Plugin;

namespace Test.Mars.Plugin;

public class NuspecHelperTests
{
    readonly string exampleNuspec = """
        <?xml version="1.0" encoding="utf-8"?>
        <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
          <metadata>
            <id>mdimai666.Mars.Plugin.PluginHost</id>
            <version>0.6.5-alpha.1</version>
            <entryAssembly>Mars.Plugin.PluginHost.dll</entryAssembly>
            <authors>mdimai666</authors>
            <license type="expression">MIT</license>
            <licenseUrl>https://licenses.nuget.org/MIT</licenseUrl>
            <icon>icon-nuget.png</icon>
            <projectUrl>https://github.com/mdimai666/Mars</projectUrl>
            <description>Mars PluginHost - backend develpment extensions;</description>
            <tags>Mars Plugin</tags>
            <repository type="git" url="https://github.com/mdimai666/Mars" commit="801b5289f4975b6df91188e32374f2cf687752d5" />
            <dependencies>
              <group targetFramework="net9.0">
                <dependency id="mdimai666.Mars.Plugin.Abstractions" version="0.6.5-alpha.1" exclude="Build,Analyzers" />
              </group>
            </dependencies>
            <frameworkReferences>
              <group targetFramework="net9.0">
                <frameworkReference name="Microsoft.AspNetCore.App" />
              </group>
            </frameworkReferences>
          </metadata>
        </package>
        """;

    [Fact]
    public void ReadFromFileContent_ShouldReturnCorrectValues()
    {
        // Arrange
        // Act
        var result = NuspecHelper.ReadFromFileContent(exampleNuspec);

        // Assert
        result.Should().NotBeNull();
        result.PackageId.Should().Be("mdimai666.Mars.Plugin.PluginHost");
        result.Version.Should().Be("0.6.5-alpha.1");
        result.Authors.Should().Contain("mdimai666");
        result.License.Should().Be("MIT");
        result.LicenseUrl.Should().Be("https://licenses.nuget.org/MIT");
        result.Icon.Should().Be("icon-nuget.png");
        result.ProjectUrl.Should().Be("https://github.com/mdimai666/Mars");
        result.Description.Should().Be("Mars PluginHost - backend develpment extensions;");
        result.Tags.Should().Contain("Mars Plugin");
        result.Repository.Type.Should().Be("git");
        result.Repository.Url.Should().Be("https://github.com/mdimai666/Mars");
        result.Repository.Commit.Should().Be("801b5289f4975b6df91188e32374f2cf687752d5");

        result.Dependencies.Count.Should().Be(1);
        var dependencyGroup = result.Dependencies.First();
        dependencyGroup.TargetFramework.Should().Be("net9.0");
        var dependency = dependencyGroup.Dependencies.First();
        dependency.Id.Should().Be("mdimai666.Mars.Plugin.Abstractions");
        dependency.Version.Should().Be("0.6.5-alpha.1");
        dependency.Exclude.Should().Be("Build,Analyzers");

    }

    [Fact]
    public void ReadFromFileContent_InvalidXml_ShouldThrowException()
    {
        // Arrange
        string invalidNuspec = "{}123";
        // Act
        Action act = () => NuspecHelper.ReadFromFileContent(invalidNuspec);
        // Assert
        act.Should().Throw<XmlException>();
    }

    [Fact]
    public void CreateNuspec_ShouldSuccess()
    {
        // Arrange
        var manifest = new NuspecManifest
        {
            PackageId = "mdimai666.Mars.Plugin.PluginHost",
            Version = "0.6.5-alpha.1",
            Authors = "mdimai666",
            License = "MIT",
            LicenseUrl = "https://licenses.nuget.org/MIT",
            Icon = "icon-nuget.png",
            ProjectUrl = "https://github.com/mdimai666/Mars",
            Description = "Mars PluginHost - backend develpment extensions;",
            Tags = "Mars Plugin",
            Repository = new NuspecRepository
            {
                Type = "git",
                Url = "https://github.com/mdimai666/Mars",
                Commit = "801b5289f4975b6df91188e32374f2cf687752d5"
            },
            Dependencies =
            [
                new NuspecDependencyGroup
                {
                    TargetFramework = "net9.0",
                    Dependencies =
                    [
                        new NuspecDependency
                        {
                            Id = "mdimai666.Mars.Plugin.Abstractions",
                            Version = "0.6.5-alpha.1",
                            Exclude = "Build,Analyzers"
                        }
                    ]
                }
            ]
        };
        var exampleManifest = NuspecHelper.ReadFromFileContent(exampleNuspec);

        // Act
        var manifestXml = NuspecHelper.CreateNuspec(manifest, "");

        // Assert
        var generatedmanifest = NuspecHelper.ReadFromFileContent(manifestXml);
        generatedmanifest.Should().BeEquivalentTo(exampleManifest);

    }
}
