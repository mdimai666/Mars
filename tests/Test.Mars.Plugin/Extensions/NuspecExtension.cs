using Mars.Plugin;

namespace Test.Mars.Plugin.Extensions;

public static class NuspecExtension
{
    public static NuspecManifest MockManifest(string packageId = "com.Project.Plugin1")
    {
        var manifest = new NuspecManifest
        {
            PackageId = packageId,
            Version = "0.6.5-alpha.1",
            Authors = packageId.Split('.')[0],
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

        return manifest;
    }
}
