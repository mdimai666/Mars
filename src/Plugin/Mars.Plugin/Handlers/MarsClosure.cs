using System.Text.Json;
using Mars.Plugin.Front.Abstractions;

namespace Mars.Plugin.Handlers;

/// <summary>
/// Множество сборок, уже входящих в замыкание Марса (по `Mars.deps.json`
/// рядом с приложением + `TRUSTED_PLATFORM_ASSEMBLIES` хоста) — по нему
/// фильтруются зависимости плагинов.
/// </summary>
internal static class MarsClosure
{
    internal static HashSet<string> ReadAssemblyNames(string depsJsonPath)
    {
        var names = ReadFromDepsJson(depsJsonPath);

        // Shared frameworks (Microsoft.AspNetCore.App и т.п.) в deps.json приложения
        // не перечисляются — они даются рантаймом, их имена есть только в
        // TRUSTED_PLATFORM_ASSEMBLIES хост-процесса.
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string tpa && tpa.Length > 0)
            foreach (var assemblyPath in tpa.Split(Path.PathSeparator))
                names.Add(Path.GetFileNameWithoutExtension(assemblyPath));

        return names;
    }

    static HashSet<string> ReadFromDepsJson(string depsJsonPath)
    {
        if (!File.Exists(depsJsonPath)) return [];

        DependenciesJsonDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<DependenciesJsonDto>(File.ReadAllText(depsJsonPath));
        }
        catch (JsonException)
        {
            return [];
        }

        if (dto?.runtimeTarget?.name is null || dto.targets is null
            || !dto.targets.TryGetValue(dto.runtimeTarget.name, out var runtimeTarget))
            return [];

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var package in runtimeTarget.Values)
        {
            if (package.runtime is null) continue;
            foreach (var runtimeFile in package.runtime.Keys)
                names.Add(Path.GetFileNameWithoutExtension(runtimeFile));
        }

        return names;
    }
}
