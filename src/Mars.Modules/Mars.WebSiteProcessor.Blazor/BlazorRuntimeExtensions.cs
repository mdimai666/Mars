using System.Reflection;
using Microsoft.AspNetCore.Builder;

namespace Mars.WebSiteProcessor.Blazor;

internal static class BlazorRuntimeExtensions
{
    internal static Assembly AddBlazorWebAssemblyRuntime(this WebApplicationBuilder builder, string dllPath)
    {
        //https://stackoverflow.com/questions/1137781/correct-way-to-load-assembly-find-class-and-call-run-method
        //////string dd = @"C:\Users\d\Documents\Projects\2022\Mars\AppFront\bin\Debug\net6.0\publish\wwwroot";
        ////string dd = @"C:\Users\d\Documents\Projects\2022\Mars\AppFront\bin\Debug\net6.0\publish\";

        string extraPath = dllPath;

        //https://stackoverflow.com/a/1373295/6723966
        AppDomain.CurrentDomain.AssemblyResolve += (object? sender, ResolveEventArgs args) =>
        {
            string folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
            //if (!File.Exists(assemblyPath)) return null;
            if (!File.Exists(assemblyPath))
            {
                string assemblyPath2 = Path.Combine(extraPath, new AssemblyName(args.Name).Name + ".dll");
                if (!File.Exists(assemblyPath2)) return null;
                else assemblyPath = assemblyPath2;
            }
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        };

        var loadedAsms = AppDomain.CurrentDomain.GetAssemblies();
        var loadedAsmsNames = loadedAsms.Select(s => s.GetName().Name);

        var asm = Assembly.LoadFile(dllPath);
        //var depends = asm.GetReferencedAssemblies();

        //List<AssemblyName> extraDepends = new();

        //foreach(var a in depends)
        //{
        //    var alreadyLoaded = loadedAsmsNames.Contains(a.Name);

        //    if (!alreadyLoaded)
        //    {
        //        extraDepends.Add(a);
        //    }
        //}
        //foreach(var a in extraDepends)
        //{
        //    Assembly.LoadFile(Path.Combine(APP_path, a.Name + ".dll"));
        //}

        //AppDomain.CurrentDomain.Load(asm.GetName());
        //Assembly.LoadFile(Path.Combine(APP_path, "ChatBotFrame" + ".dll"));

        return asm;
    }
}
