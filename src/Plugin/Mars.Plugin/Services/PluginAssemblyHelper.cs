using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace Mars.Plugin.Services;

internal static class PluginAssemblyHelper
{
    public static bool IsRazorClassLibrary(Assembly assembly)
    {
        return assembly.GetCustomAttributes()
            .Any(attr => attr.GetType().FullName == "Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute"
                      || attr.GetType().FullName == "Microsoft.AspNetCore.Mvc.ApplicationParts.RelatedAssemblyAttribute");
    }

    public static bool IsBlazorAssembly(Assembly assembly)
    {
        // Проверяем, ссылается ли сборка на базовую библиотеку Blazor
        bool referencesBlazor = assembly.GetReferencedAssemblies()
            .Any(a => a.Name != null && a.Name.StartsWith("Microsoft.AspNetCore.Components"));

        if (!referencesBlazor) return false;

        // Для надежности проверяем, есть ли внутри типы, реализующие интерфейс компонента Blazor
        try
        {
            return assembly.GetTypes().Any(t =>
                t.IsClass &&
                !t.IsAbstract &&
                t.GetInterface("Microsoft.AspNetCore.Components.IComponent") != null);
        }
        catch (ReflectionTypeLoadException)
        {
            // На случай, если какие-то зависимости плагина еще не загружены в контекст
            return false;
        }
    }

    public static Assembly[] ReadFrontAssemblies(Assembly mainAssembly)
    {
        var context = DependencyContext.Load(mainAssembly);

        if (context == null) return [];

        // Вытаскиваем только те библиотеки, у которых тип в deps.json указан как "project"
        var projectRuntimeLibraries = context.RuntimeLibraries
            .Where(lib => lib.Type == "project");

        var assemblies = new List<Assembly>();
        foreach (var runtimeLib in projectRuntimeLibraries)
        {
            try
            {
                // Загружаем по имени, так как они точно локальные проекты
                var asm = Assembly.Load(new AssemblyName(runtimeLib.Name));
                assemblies.Add(asm);
            }
            catch { }
        }

        return assemblies.ToArray();
    }

    public static bool IsAssemblyDebugBuild(Assembly assembly)
    {
        // Ищем DebuggableAttribute в метаданных сборки
        var debugAttribute = assembly.GetCustomAttribute<DebuggableAttribute>();

        if (debugAttribute == null)
        {
            // Если атрибута вообще нет — это гарантированно Release
            return false;
        }

        // Проверяем флаг оптимизации кода. 
        // Если IsJITOptimizerDisabled == true, значит оптимизация отключена (это Debug)
        return debugAttribute.IsJITOptimizerDisabled;
    }
}
