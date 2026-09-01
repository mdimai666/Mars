using System.Text.Json;
using Mars.Plugin.Front.Abstractions;

namespace Mars.Plugin.Handlers;

/// <summary>
/// Множество сборок, уже входящих в замыкание Марса (по `Mars.deps.json`
/// рядом с приложением) — по нему фильтруются зависимости плагинов.
/// </summary>
internal static class MarsClosure
{
    internal static HashSet<string> ReadAssemblyNames(string depsJsonPath)
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
