using System.Reflection;
using Mars.Plugin.Sdk.Models;

namespace Mars.Plugin.Sdk;

internal class PreparePublishData
{
    // Пакет самого инструмента: исключается из зависимостей nuspec и из бандла
    // (в deps.json он ключуется package id, а не assembly name).
    internal const string MarsSdkPackageId = "mdimai666.Mars.Plugin.Sdk";

    // Пакеты экосистемы Марса, которых нет в замыкании Mars.WebApp, но которые плагин
    // может ссылать; их сборки стрипаются как марсовые.
    internal static readonly HashSet<string> MarsAddonPackages = new(StringComparer.OrdinalIgnoreCase)
    {
        MarsSdkPackageId,
        "mdimai666.Mars.Plugin.Kit.Host",
        "mdimai666.Mars.Plugin.Kit.Front",
    };

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

        Console.WriteLine("POST PUBLISH SCRIPT: start");
        Console.WriteLine("dir=" + Directory.GetCurrentDirectory());
        Console.WriteLine("assemblyFolder=" + assemblyFolder);

        var marsReleaseDepsJsonFile = Path.Combine(assemblyFolder, "Mars.deps.json");
        _marsWebAppDependencies = new ProjectDependencies(marsReleaseDepsJsonFile);

        var releaseArtifactsDepsjsonFile = Path.Combine(Settings.ProjectDir, Settings.OutDir, Settings.ProjectName + ".deps.json");
        ProjectDependencies = new ProjectDependencies(releaseArtifactsDepsjsonFile);

        var webApp = RecurseDependiesList(_marsWebAppDependencies.Packages["Mars"], _marsWebAppDependencies, _ => true, true);
        // «Пакет Марса» — по фактическому замыканию, а не по имени: сборки из Mars.deps.json
        // хост отдаёт сам (стрипаются), плюс явные аддоны экосистемы вне замыкания WebApp.
        // Имя автора плагина на классификацию не влияет.
        var projectDepends = RecurseDependiesList(ProjectDependencies.Packages[Settings.ProjectName], ProjectDependencies,
            name => webApp.marsDepends.ContainsKey(name) || MarsAddonPackages.Contains(name), false);

        HashSet<string> devTools = [MarsSdkPackageId, "Microsoft.AspNetCore.Components.WebAssembly.DevServer"];

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
