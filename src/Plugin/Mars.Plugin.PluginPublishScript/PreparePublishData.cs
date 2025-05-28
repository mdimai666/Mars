using System.Reflection;
using Mars.Plugin.PluginPublishScript.Models;

namespace Mars.Plugin.PluginPublishScript;

internal class PreparePublishData
{
    public readonly ProjectDependencies _marsWebAppDependencies;
    public ProjectDependencies ProjectDependencies;

    public readonly string ToolAssemblyName = Assembly.GetExecutingAssembly().GetName().Name!;

    public ProcessScriptSettings Settings;

    /// <summary>
    /// Очищенная от зависимостей, который содержатся в Mars.WebApp. Чтобы не дублировать файлы.
    /// </summary>
    public Dictionary<string, Library> ProjectSelfDepends { get; }
    public Dictionary<string, Library> MarsLibraries { get; }

    public PreparePublishData(string[] args)
    {
        string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        Settings = new ProcessScriptSettings(args);

        Console.WriteLine("args: " + string.Join(' ', args));
        //Console.WriteLine("env: " + string.Join(',', EnvVa()));

        Console.WriteLine("POST PUBLISH SCRIPT: start");
        Console.WriteLine("dir=" + Directory.GetCurrentDirectory());
        Console.WriteLine("assemblyFolder=" + assemblyFolder);

        var marsReleaseDepsJsonFile = Path.Combine(assemblyFolder, "Mars.deps.json");
        _marsWebAppDependencies = new ProjectDependencies(marsReleaseDepsJsonFile);

        //var releaseArtifactsDepsjsonFile = @"C:\Users\D\Documents\VisualStudio\2025\Mars.TelegramPlugin\Mars.TelegramPlugin\obj\Release\net9.0\Mars.TelegramPlugin.deps.json";
        //var releaseArtifactsDepsjsonFile = Path.Combine(ProjectDir, OutDir.Replace("bin\\", "obj\\"), ProjectName + ".deps.json");
        var releaseArtifactsDepsjsonFile = Path.Combine(Settings.ProjectDir, Settings.OutDir, Settings.ProjectName + ".deps.json");
        ProjectDependencies = new ProjectDependencies(releaseArtifactsDepsjsonFile);

        var webApp = RecurseDependiesList(_marsWebAppDependencies.Packages["Mars"], _marsWebAppDependencies, _ => true, true);
        var projectDepends = RecurseDependiesList(ProjectDependencies.Packages[Settings.ProjectName], ProjectDependencies, MarsNugets.Contains, false);

        HashSet<string> devTools = [ToolAssemblyName, "Microsoft.AspNetCore.Components.WebAssembly.DevServer"];

        MarsLibraries = webApp.marsDepends.Concat(projectDepends.marsDepends).DistinctBy(s => s.Key).ToDictionary();

        // Это мы добавляем элементы, которые Mars.WebApp не ссылается, но они есть в виде nuget для плагинов
        // к примеру Mars.Plugin.Kit.Host, Mars.Plugin.Kit.Front
        foreach (var package in projectDepends.marsDepends)
        {
            MarsLibraries.TryAdd(package.Key, package.Value);

            _marsWebAppDependencies.Libraries.TryAdd(package.Key, package.Value);

            if (_marsWebAppDependencies.Packages.ContainsKey(package.Key))
            {
                var md = _marsWebAppDependencies.Packages[package.Key];
                var pd = ProjectDependencies.Packages[package.Key];
                foreach (var d in pd.Runtime)
                {
                    md.Runtime.TryAdd(d.Key, d.Value);
                }
                foreach (var d in pd.Resources)
                {
                    md.Resources.TryAdd(d.Key, d.Value);
                }
            }
            else _marsWebAppDependencies.Packages.Add(package.Key, ProjectDependencies.Packages[package.Key]);
        }

        ProjectSelfDepends = projectDepends.otherPackages.Where(s => !webApp.marsDepends.ContainsKey(s.Key) && !devTools.Contains(s.Key)).ToDictionary();
    }

    IEnumerable<string> EnvVa()
    {
        var env = Environment.GetEnvironmentVariables();
        foreach (var key in env.Keys)
        {
            yield return $"{key}={env[key]}";
        }
    }

    private static readonly string[] MarsNugetsDefinition = [
        // shared
        "Mars.Core",
        "Mars.Shared",
        "Mars.Options/Mars.Options",
        "AppFront.Shared",
        "AppFront.Main",
        // host
        "Mars.Host.Shared",
        "Mars.Host.Data",
        "Mars.Shared",
        // nodes
        "Mars.Nodes/Mars.Nodes.Core",
        "Mars.Nodes/Mars.Nodes.Core.Implements",
        "Mars.Nodes/Mars.Nodes.EditorApi",
        "Mars.Nodes/Mars.Nodes.FormEditor",
        // modules
        "Modules/MarsEditors",
        "Modules/MarsCodeEditor2",
        "Mars.WebApiClient",
        "Modules/BlazoredHtmlRender",
        // plugin
        "Plugin/Mars.Plugin.Abstractions",
        "Plugin/Mars.Plugin.Front",
        "Plugin/Mars.Plugin.Kit.Host",
        "Plugin/Mars.Plugin.Kit.Front",
        "Plugin/Mars.Plugin.PluginHost",
        "Plugin/Mars.Plugin.Front.Abstractions",
        "Plugin/Mars.Plugin.PluginPublishScript"
        ];

    //private static readonly string[] MarsReferenceProjects = ["AppAdmin"];

    private static readonly HashSet<string> MarsNugets = MarsNugetsDefinition.Select(s => "mdimai666." + (s.Contains('/') ? s.Split("/", 2)[1] : s)).ToHashSet();

    public (Dictionary<string, Library> marsDepends, Dictionary<string, Library> otherPackages)
            RecurseDependiesList(Dependency dependency,
            ProjectDependencies projectDependencies,
            Func<string, bool> checkProjectPartOfTheMarsFunc,
            bool isScanMars)
    {
        var marsDepends = new Dictionary<string, Library>();
        var otherPackages = new Dictionary<string, Library>();
        var collectedDependencies = new HashSet<string>();

        var currentDep = projectDependencies.Libraries[dependency.Name];
        if (isScanMars) marsDepends.Add(dependency.Name, currentDep);
        else otherPackages.Add(dependency.Name, currentDep);

        FindAllDependencies(dependency, projectDependencies, marsDepends, otherPackages, false, collectedDependencies, checkProjectPartOfTheMarsFunc);

        if (!isScanMars)
        {
            foreach (var item in marsDepends.Keys)
            {
                otherPackages.Remove(item);
            }
        }

        return (marsDepends, otherPackages);
    }

    private void FindAllDependencies(Dependency dep,
                                    ProjectDependencies projectDependencies,
                                    Dictionary<string, Library> marsDepends,
                                    Dictionary<string, Library> otherPackages,
                                    bool isMarsDep,
                                    HashSet<string> collectedDependencies,
                                    Func<string, bool> checkProjectPartOfTheMarsFunc)
    {
        collectedDependencies.Add(dep.Name);

        foreach (var packageEntry in dep.Dependencies.Values)
        {
            if (collectedDependencies.Contains(packageEntry.Name)) continue;
            var checkIsMarsDep = isMarsDep || checkProjectPartOfTheMarsFunc(packageEntry.Name); //AllMarsProject.Contains(packageEntry.Name);
            var lib = projectDependencies.Libraries[packageEntry.Name];
            if (checkIsMarsDep) marsDepends.TryAdd(packageEntry.Name, lib);
            else if (!marsDepends.ContainsKey(packageEntry.Name)) otherPackages.TryAdd(packageEntry.Name, lib);

            var depe2 = projectDependencies.Packages[packageEntry.Name];
            if (depe2.Dependencies == null) continue;

            FindAllDependencies(depe2, projectDependencies, marsDepends, otherPackages, checkIsMarsDep, collectedDependencies, checkProjectPartOfTheMarsFunc);
        }
    }
}
