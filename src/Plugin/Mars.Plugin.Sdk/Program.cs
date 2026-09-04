using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using Mars.Plugin.Sdk;
using Mars.Plugin.Sdk.Models;

// Mars.Plugin.Sdk — инструмент паковки плагинов Марса.
//
//   pack zip       — после publish: отсекает сборки, уже есть в Марсе, пишет дескриптор
//                    mars-plugin.json и собирает zip;
//   pack nuget     — то же + nupkg классического лейаута (lib/ + mars/); зависимости
//                    объявляются в nuspec и резолвятся самим Марсом при установке.
//
// Фронт-манифест `_front_plugins.json` намеренно НЕ пакуется: его генерирует сервер на
// лету (PluginManifestProvider) из <Плагин>.staticwebassets.endpoints.json с фильтром по
// замыканию админки — файл из пакета никем не читается.

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

var mode = args[0].ToLowerInvariant() switch
{
    "pack" when args.Length > 1 && args[1].Equals("zip", StringComparison.OrdinalIgnoreCase) => ProcessMode.PackZip,
    "pack" when args.Length > 1 && args[1].Equals("nuget", StringComparison.OrdinalIgnoreCase) => ProcessMode.PackNuget,
    _ => ProcessMode.Undefinded
};

if (mode == ProcessMode.Undefinded)
{
    PrintUsage();
    return 1;
}

Console.WriteLine($" -> Mars.Plugin.Sdk: {args[0]} {(args.Length > 1 ? args[1] : "")}");

//////////////////////////////////////////////////
//// Prepare
//////////////////////////////////////////////////
Console.WriteLine("[1/4] Prepare...");

var data = new PreparePublishData(args);
var settings = data.Settings;
var outDir = new DirectoryInfo(Path.Combine(settings.ProjectDir, settings.OutDir));

// Заранее надо просчитать чтобы вычислить _frameworkFilesRemove
var marsDlls = ScriptFilesProcessing.CalculateDlls(data.MarsLibraries, data._marsWebAppDependencies).ToHashSet();
var selfDlls = ScriptFilesProcessing.CalculateDlls(data.ProjectSelfDepends, data.ProjectDependencies).ToHashSet();

//remove wwwroot/_content
var _content = new DirectoryInfo(Path.Combine(outDir.FullName, "wwwroot", "_content"));
var _contentDirs = _content.Exists ? _content.GetDirectories() : [];
var _contentDirsRemove = _contentDirs.Where(d => data.MarsLibraries.ContainsKey(d.Name)).ToList();

//remove wwwroot/_framework
var _framework = new DirectoryInfo(Path.Combine(outDir.FullName, "wwwroot", "_framework"));
var _frameworkFiles = _framework.Exists ? _framework.GetFiles("*", SearchOption.AllDirectories).ToDictionary(s => s.FullName) : [];
var _frameworkFilesRemove = new HashSet<string>();

string[] compressExts = [".gz", ".br"];

var _otherArtifacts = new HashSet<string>();

foreach (var f in _frameworkFiles.Values)
{
    //системный файл
    if (ScriptFilesProcessing.IsFrameworkSystemFile(f.Name)) _frameworkFilesRemove.Add(f.FullName);

    else if (f.Extension == ".wasm")
    {
        var packageName = string.Join('.', f.Name.Split('.')[..^2]);
        if (packageName.EndsWith(".resources")) packageName = packageName.Substring(0, packageName.Length - ".resources".Length);
        if (data.MarsLibraries.ContainsKey(packageName) || marsDlls.Contains(packageName + ".dll"))
        {
            _frameworkFilesRemove.Add(f.FullName);
            foreach (var z in compressExts)
                if (_frameworkFiles.TryGetValue(f.FullName + z, out var fz)) _frameworkFilesRemove.Add(fz.FullName);
        }
    }
}

var dlls = outDir.GetFiles("*.dll", SearchOption.AllDirectories);
var dllsNames = dlls.Select(s => s.FullName).ToHashSet();
var otherFiles = outDir.GetFiles("*", SearchOption.AllDirectories).Where(f => !dllsNames.Contains(f.FullName)).ToList();
var allFilesCount = otherFiles.Count + dllsNames.Count;

foreach (var x in _contentDirsRemove)
{
    otherFiles = otherFiles.Where(s => !s.FullName.StartsWith(x.FullName)).ToList();
}

otherFiles = otherFiles.Where(s => !_frameworkFilesRemove.Contains(s.FullName)).ToList();

// remove tool myself
_otherArtifacts = _otherArtifacts.Concat(otherFiles.Where(s => s.Name.StartsWith(data.ToolAssemblyName)).Select(s => s.FullName)).ToHashSet();

// debug symbols
_otherArtifacts = _otherArtifacts.Concat(otherFiles.Where(s => s.Name.EndsWith(".pdb")).Select(s => s.FullName)).ToHashSet();

// Рантайму нужен <Плагин>.staticwebassets.endpoints.json рядом со сборкой (читает
// PluginManifestProvider), поэтому удаляются только чужие и дев-манифесты.
var keepEndpointsJson = settings.ProjectName + ".staticwebassets.endpoints.json";
_otherArtifacts = _otherArtifacts.Concat(otherFiles.Where(s => s.Name.EndsWith(".staticwebassets.runtime.json")
                                                            || (s.Name.EndsWith(".staticwebassets.endpoints.json") && s.Name != keepEndpointsJson))
                                    .Select(s => s.FullName)).ToHashSet();

otherFiles = otherFiles.Where(s => !_otherArtifacts.Contains(s.FullName)).ToList();

ScriptFilesProcessing.SomeChecks(marsDlls, data);

var thirdPartyDlls = new List<FileInfo>();

foreach (var dll in dlls)
{
    var relPath = Path.GetRelativePath(outDir.FullName, dll.FullName);
    var keyPath = relPath.Replace("\\", "/");
    if (selfDlls.Contains(keyPath)) otherFiles.Add(dll);
    else if (!marsDlls.Contains(keyPath)) thirdPartyDlls.Add(dll);
}

//////////////////////////////////////////////////
//// Calculate files
//////////////////////////////////////////////////
Console.WriteLine("[2/4] Calculate complete!");
Console.WriteLine($"""
    files in publish dir: {allFilesCount};
    files to publish: {otherFiles.Count};
    """);

Console.ForegroundColor = ConsoleColor.Green;
foreach (var file in otherFiles)
{
    var relPath = Path.GetRelativePath(outDir.FullName, file.FullName);
    Console.WriteLine($"\t{relPath}");
}
Console.ResetColor();

if (thirdPartyDlls.Count > 0)
{
    Console.WriteLine("Third-party dependencies bundled with the plugin (absent from Mars):");
    Console.ForegroundColor = ConsoleColor.Yellow;
    foreach (var file in thirdPartyDlls)
        Console.WriteLine($"\t{Path.GetRelativePath(outDir.FullName, file.FullName)}");
    Console.ResetColor();
    otherFiles.AddRange(thirdPartyDlls);
}

//////////////////////////////////////////////////
//// Remove shared Mars files
//////////////////////////////////////////////////
Console.WriteLine("[3/4] Remove shared Mars files...");

_contentDirsRemove.ForEach(d => d.Delete(true));

var hash = otherFiles.Select(s => s.FullName).ToHashSet();
var toRemoveFiles = outDir.GetFiles("*", SearchOption.AllDirectories).Where(f => !hash.Contains(f.FullName)).ToList();
toRemoveFiles.ForEach(f => f.Delete());

var dirs = outDir.GetDirectories().ToList();
foreach (var d in dirs)
    if (!d.GetDirectories().Any() && !d.GetFiles().Any())
        d.Delete();

if (_framework.Exists)
{
    var _frameworkFilesDirFilesCount = _framework.GetFiles("*", SearchOption.AllDirectories).Count();
    if (_frameworkFilesDirFilesCount == 0) _framework.Delete(true);
}

if (_content.Exists)
{
    var _contentFilesDirFilesCount = _content.GetFiles("*", SearchOption.AllDirectories).Count();
    if (_contentFilesDirFilesCount == 0) _content.Delete(true);
}

//////////////////////////////////////////////////
//// Descriptor + package
//////////////////////////////////////////////////
// zip: иконка кладётся в wwwroot/ — рантайм сервит её через /_plugin/<key>/
string? zipIconFile = null;
if (mode == ProcessMode.PackZip && !string.IsNullOrWhiteSpace(settings.Icon))
{
    var iconSource = new[] { Path.Combine(outDir.FullName, settings.Icon), Path.Combine(outDir.FullName, "wwwroot", settings.Icon) }
        .FirstOrDefault(File.Exists);
    if (iconSource is not null)
    {
        var wwwrootDir = Directory.CreateDirectory(Path.Combine(outDir.FullName, "wwwroot"));
        var iconDestination = Path.Combine(wwwrootDir.FullName, Path.GetFileName(settings.Icon));
        if (!Path.Equals(iconSource, iconDestination))
            File.Copy(iconSource, iconDestination, overwrite: true);
        zipIconFile = Path.GetFileName(settings.Icon);
    }
}

var descriptorPath = PluginDescriptorWriter.Write(outDir, settings, zipIconFile);

if (mode == ProcessMode.PackZip)
{
    var zipPath = PluginPackageBuilder.BuildZip(outDir, settings);
    Console.WriteLine($"Plugin zip: {zipPath}");
}
else if (mode == ProcessMode.PackNuget)
{
    var nupkgPath = PluginPackageBuilder.BuildNuget(outDir, settings, data, selfDlls, descriptorPath);
    Console.WriteLine($"Plugin nupkg: {nupkgPath}");
}

Console.WriteLine("FINISH");
return 0;

static void PrintUsage()
{
    Console.WriteLine("""
        Mars.Plugin.Sdk — Mars plugin packaging tool.

        Usage:
          dotnet Mars.Plugin.Sdk.dll pack zip --ProjectName=<Name> --out=<publishDir> --ProjectDir=<dir> --PackageId=<id> --Version=<ver>
          dotnet Mars.Plugin.Sdk.dll pack nuget --ProjectName=<Name> --out=<publishDir> --ProjectDir=<dir> --PackageId=<id> --Version=<ver>
                                              [--Authors=..] [--Title=..] [--Description=..] [--Tags=..] [--Icon=icon.png]
                                              [--LicenseExpression=..] [--LicenseFile=..] [--ProjectUrl=..]
                                              [--RepositoryUrl=..] [--RepositoryType=..] [--RepositoryBranch=..] [--RepositoryCommit=..]
                                              [--ReadmeFile=..] [--Copyright=..] [--ReleaseNotes=..]

        Normally invoked by the MSBuild targets of the mdimai666.Mars.Plugin.Sdk package:
          dotnet publish -c Release                 -> zip
          dotnet msbuild -t:MarsPluginPackNuget -c Release   -> nupkg
        """);
}
