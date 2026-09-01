using System.Reflection;
using System.Runtime.Loader;
using Mars.Plugin.Handlers;

namespace Mars.Plugin.Services;

/// <summary>
/// Изолированный контекст загрузки плагина: сборки из его папки резолвятся самим
/// плагином, сборки Марса — дефолтным контекстом (тип-идентичность с хостом).
/// Это позволяет плагинам иметь собственные версии сторонних библиотек.
/// </summary>
internal sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly string _pluginDir;
    private readonly HashSet<string> _marsAssemblyNames;

    public PluginLoadContext(string pluginPath)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
        _pluginDir = Path.GetDirectoryName(Path.GetFullPath(pluginPath))!;
        _marsAssemblyNames = MarsClosure.ReadAssemblyNames(Path.Combine(AppContext.BaseDirectory, "Mars.deps.json"));
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // сборки Марса — из дефолтного контекста (один экземпляр на всех)
        if (assemblyName.Name is not null && _marsAssemblyNames.Contains(assemblyName.Name))
            return Default.Assemblies.FirstOrDefault(a => a.GetName().Name == assemblyName.Name);

        // по deps.json плагина
        var path = _resolver.ResolveAssemblyToPath(assemblyName);

        // фолбэк: файл рядом с входной сборкой (плагин без deps.json)
        if (path is null && assemblyName.Name is not null)
        {
            var candidate = Path.Combine(_pluginDir, assemblyName.Name + ".dll");
            if (File.Exists(candidate)) path = candidate;
        }

        return path is not null ? LoadFromAssemblyPath(path) : null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path is not null ? LoadUnmanagedDllFromPath(path) : IntPtr.Zero;
    }
}
