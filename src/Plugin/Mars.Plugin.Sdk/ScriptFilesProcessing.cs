using Mars.Plugin.Sdk.Models;

namespace Mars.Plugin.Sdk;

internal static class ScriptFilesProcessing
{
    internal static string[] CalculateDlls(Dictionary<string, Library> libraries, ProjectDependencies projectDependencies)
    {
        var runtimes = libraries.Keys
                           .SelectMany(s => projectDependencies.Packages.GetValueOrDefault(s)?.Runtime.Select(x => Path.GetFileName(x.Key)) ?? [])
                           .Where(s => !string.IsNullOrEmpty(s)).ToList();

        var resources = libraries.Keys
                        .SelectMany(s => projectDependencies.Packages.GetValueOrDefault(s)?.Resources.Select(x => $"{x.Value.Locale}/{Path.GetFileName(x.Key)}") ?? [])
                        .Where(s => !string.IsNullOrEmpty(s)).ToList();

        return runtimes.Concat(resources).ToArray();
    }

    internal static void SomeChecks(HashSet<string> marsDlls, PreparePublishData data)
    {
        var containProcessingProject = data.ProjectSelfDepends.ContainsKey(data.Settings.ProjectName);
        var kit1 = marsDlls.Contains("Mars.Plugin.Kit.Host.dll");
        var kit2 = marsDlls.Contains("Mars.Plugin.Kit.Front.dll");

        if (!(kit1 && kit2)) throw new Exception("Kits must be present");
        if (!containProcessingProject) throw new Exception("Target project not present");

        //var m1 = data.ProjectDependencies.Packages["System.CommandLine"];
        //var m2 = data._marsWebAppDependencies.Packages["System.CommandLine"];
    }

    internal static bool IsFrameworkSystemFile(string filename)
    {
        if (filename.StartsWith("icudt_")) return true;
        else if (filename.StartsWith("blazor.boot")) return true;
        else if (filename.StartsWith("dotnet.js")) return true;
        else if (filename.StartsWith("dotnet.native")) return true;
        else if (filename.StartsWith("dotnet.runtime")) return true;
        else if (filename.StartsWith("netstandard.")) return true;
        else if (filename.StartsWith("mscorlib.")) return true;
        else if (filename.StartsWith("System.")) return true;
        else if (filename.StartsWith("Microsoft.VisualBasic")) return true;
        else if (filename.StartsWith("WindowsBase")) return true;
        else if (filename.StartsWith("Microsoft.Win32")) return true;

        return false;
    }
}
