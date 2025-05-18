using Mars.Plugin.Front.Models;
using Mars.Plugin.PluginPublishScript;
using Mars.Plugin.PluginPublishScript.Models;

Console.WriteLine($" -> Mars plugin prepare script! \n{string.Join(',', args)}");

//string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
var marsReleaseDepsJsonFile = Path.Combine("Mars.deps.json");
var marsDepsExist = File.Exists(marsReleaseDepsJsonFile);

// Debug files
//args = "--run-postdebugcompile --ProjectName=Mars.TelegramPlugin --out=bin\\Debug\\net9.0\\  --ProjectDir=C:\\Users\\D\\Documents\\VisualStudio\\2025\\Mars.TelegramPlugin\\Mars.TelegramPlugin\\".Split(' ');

//args = "--run-postpublish --ProjectName=Mars.TelegramPlugin --out=bin\\Release\\net9.0\\publish  --ProjectDir=C:\\Users\\D\\Documents\\VisualStudio\\2025\\Mars.TelegramPlugin\\Mars.TelegramPlugin\\".Split(' ');

var mode = ProcessMode.Undefinded;
if (args[0] == "--run-postdebugcompile") mode = ProcessMode.PostDebugCompile;
else if (args[0] == "--run-postpublish") mode = ProcessMode.PostPublish;
else
{
    Console.WriteLine("POST PUBLISH SCRIPT: cannot start without arguments - EXIT!");
    Console.WriteLine("help: must be one of [--run-postpublish, --run-postdebugcompile]");
    return;
}

Console.WriteLine("PublicFilesScript Start!");
Console.WriteLine("[1/4] Preapare...");

var data = new PreparePublishData(args);
var outDir = new DirectoryInfo(Path.Combine(data.Settings.ProjectDir, data.Settings.OutDir));
var wwwroot = new DirectoryInfo(Path.Combine(outDir.FullName, "wwwroot"));

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

//Заранее надо просчитать чтобы вычислить _frameworkFilesRemove
var marsDlls = ScriptFilesProcessing.CalculateDlls(data.MarsLibraries, data._marsWebAppDependencies).ToHashSet();
var selfDlls = ScriptFilesProcessing.CalculateDlls(data.ProjectSelfDepends, data.ProjectDependencies).ToHashSet();


foreach (var f in _frameworkFiles.Values)
{
    //системный файл
    if (ScriptFilesProcessing.IsFrameworkSystemFile(f.Name)) _frameworkFilesRemove.Add(f.FullName);

    else if (f.Extension == ".wasm")
    {
        var packageName = string.Join('.', f.Name.Split('.')[..^2]);
        if (packageName.EndsWith(".resources")) packageName = packageName.Substring(0, packageName.Length - ".resources".Length);
        if (data.MarsLibraries.ContainsKey(packageName) || marsDlls.Contains(packageName + ".dll")/*тут runtimes надо искать*/)
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

foreach (var x in _frameworkFilesRemove)
{
    otherFiles = otherFiles.Where(s => !_frameworkFilesRemove.Contains(s.FullName)).ToList();
}

// remove tool myself
_otherArtifacts = _otherArtifacts.Concat(otherFiles.Where(s => s.Name.StartsWith(data.ToolAssemblyName)).Select(s => s.FullName)).ToHashSet();

// debug symbols
_otherArtifacts = _otherArtifacts.Concat(otherFiles.Where(s => s.Name.EndsWith(".pdb")).Select(s => s.FullName)).ToHashSet();

//remove knowns jsons
_otherArtifacts = _otherArtifacts.Concat(otherFiles.Where(s => s.Name.EndsWith(".staticwebassets.runtime.json")
                                                            || s.Name.EndsWith(".staticwebassets.endpoints.json"))
                                    .Select(s => s.FullName)).ToHashSet();

HashSet<string> generateFilesNames = [Path.Combine(wwwroot.FullName, MarsFrontPluginManifest.DefaultManifestFileName)];

otherFiles = otherFiles.Where(s => !_otherArtifacts.Contains(s.FullName)).ToList();
otherFiles = otherFiles.Where(s => !generateFilesNames.Contains(s.FullName)).ToList();

//dlls was here
ScriptFilesProcessing.SomeChecks(marsDlls, data);

var unknownDlls = new List<FileInfo>();

foreach (var dll in dlls)
{
    var relPath = Path.GetRelativePath(outDir.FullName, dll.FullName);
    var keyPath = relPath.Replace("\\", "/");
    if (selfDlls.Contains(keyPath)) otherFiles.Add(dll);
    if (marsDlls.Contains(keyPath) || selfDlls.Contains(keyPath)) continue;
    unknownDlls.Add(dll);
}
if (unknownDlls.Count > 0)
{
    Console.WriteLine("found unknown Dlls:");
    Console.WriteLine(string.Join(',', unknownDlls.Select(s => s.Name)));
    throw new Exception("found unknown Dlls");
}

//allowDllsPath.AddRange()

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


//BEGIN 
//if (isCopy)
//{
//    Console.WriteLine("[3/4] Copy...");
//    string plugin_publishDirName = "plugin_publish";
//    var plugin_publish = new DirectoryInfo(Path.Combine(data.Settings.ProjectDir, data.Settings.OutDir, plugin_publishDirName));
//    plugin_publish.Create();

//    foreach (var f in otherFiles)
//    {
//        var rel = Path.GetRelativePath(plugin_publish.FullName, f.FullName);
//        f.CopyTo(Path.Combine(plugin_publish.FullName, rel), true);
//    }
//}
if (mode == ProcessMode.PostDebugCompile)
{
    Console.WriteLine("[3/3] Remove shared Mars files : not required");
}
else if (mode == ProcessMode.PostPublish)
{
    Console.WriteLine("[3/3] Remove shared Mars files...");

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

}

//Prepare Manifest file
Console.WriteLine("[3/4] Create new manifest file ");

if (mode == ProcessMode.PostDebugCompile)
{
    ManifestProcessing.ProcessDebugManifest(data.Settings, data._marsWebAppDependencies, data.ProjectDependencies, marsDlls);

}
else if (mode == ProcessMode.PostPublish)
{
    ManifestProcessing.ProcessPublishManifest(data.Settings, data._marsWebAppDependencies, data.ProjectDependencies, otherFiles, outDir, wwwroot, marsDlls);
}
else
{
    throw new NotImplementedException($"mode = '{mode}' not implement");
}

Console.WriteLine("FINISH");
