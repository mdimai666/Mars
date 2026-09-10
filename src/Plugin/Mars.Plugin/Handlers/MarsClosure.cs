using System.Text.Json;
using Mars.Plugin.Front.Abstractions;

namespace Mars.Plugin.Handlers;

/// <summary>
/// Множество сборок и пакетов, уже входящих в замыкание Марса (по `Mars.deps.json`
/// рядом с приложением + `TRUSTED_PLATFORM_ASSEMBLIES` хоста) — по нему
/// фильтруются зависимости плагинов.
/// </summary>
internal static class MarsClosure
{
    internal static HashSet<string> ReadAssemblyNames(string depsJsonPath)
    {
        var names = ReadFromDepsJson(depsJsonPath)?.assemblies ?? [];

        // Shared frameworks (Microsoft.AspNetCore.App и т.п.) в deps.json приложения
        // не перечисляются — они даются рантаймом, их имена есть только в
        // TRUSTED_PLATFORM_ASSEMBLIES хост-процесса.
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string tpa && tpa.Length > 0)
            foreach (var assemblyPath in tpa.Split(Path.PathSeparator))
                names.Add(Path.GetFileNameWithoutExtension(assemblyPath));

        return names;
    }

    /// <summary>Id пакетов в замыкании Марса (ключи таргета `PackageId/Version` в deps.json).</summary>
    internal static HashSet<string> ReadClosurePackageIds(string depsJsonPath)
        => ReadFromDepsJson(depsJsonPath)?.packageIds ?? [];

    static (HashSet<string> assemblies, HashSet<string> packageIds)? ReadFromDepsJson(string depsJsonPath)
    {
        if (!File.Exists(depsJsonPath)) return null;

        DependenciesJsonDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<DependenciesJsonDto>(File.ReadAllText(depsJsonPath));
        }
        catch (JsonException)
        {
            return null;
        }

        if (dto?.runtimeTarget?.name is null || dto.targets is null
            || !dto.targets.TryGetValue(dto.runtimeTarget.name, out var runtimeTarget))
            return null;

        var assemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var packageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (packageKey, package) in runtimeTarget)
        {
            var slash = packageKey.IndexOf('/');
            packageIds.Add(slash > 0 ? packageKey[..slash] : packageKey);

            if (package.runtime is null) continue;
            foreach (var runtimeFile in package.runtime.Keys)
                assemblies.Add(Path.GetFileNameWithoutExtension(runtimeFile));
        }

        return (assemblies, packageIds);
    }
}
