using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.RegularExpressions;
using Mars.Shared.Models.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Mars.Shared.Tools;

public class ModelProperySel
{
    public required ModelInfo Model { get; init; }
    public required PropertyInfo Property { get; init; }

}

public class ModelInfo
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required Type ModelType { get; init; }
    public required PropertyInfo[] Properties { get; init; }

    public required string SourceFilePath { get; init; }

    //public Dictionary<string, string> BadgeHtmlAttributes { get; init; }

    public static string ShortName(string typeName)
    {
        return typeName switch
        {
            "Int32" => "int",
            "Int64" => "int64",
            "String" => "string",
            "Boolean" => "bool",
            _ => typeName
        };
    }

    public static Dictionary<string, object> BadgeHtmlAttributes(Type t)
    {
        var d = new Dictionary<string, object>();

        if (t.IsEnum) d.Add(nameof(t.IsEnum), "");
        if (t.IsClass) d.Add(nameof(t.IsClass), "");
        if (t.IsArray) d.Add(nameof(t.IsArray), "");
        if (t.IsPrimitive) d.Add(nameof(t.IsPrimitive), "");
        if (t.IsCollectible) d.Add(nameof(t.IsCollectible), "");

        //string attr2 => $"{(Value.IsEnum ? "IsEnum" : "")} {(Value.IsClass ? "IsClass" : "")} {(Value.IsPrimitive ? "IsPrimitive" : "")} {(Value.IsArray ? "IsArray" : "")}";

        return d;
    }

}

public class ModelInfoService
{
    List<Type>? _registeredModelsTypes = null;
    public List<Type> RegisteredModelsTypes
    {
        get
        {
            if (_registeredModelsTypes == null)
            {
                _registeredModelsTypes = GetInterfaceImplements<IBasicEntity>(true).ToList();
            }
            return _registeredModelsTypes;
        }
    }
    List<ModelInfo>? _registeredModels = null;
    public List<ModelInfo> RegisteredModels
    {
        get
        {

            if (_registeredModels == null)
            {
                _registeredModels = RegisteredModelsTypes.Select(x =>
                {
                    //var assembly = Assembly.GetAssembly(x);
                    //string sourceFilePath = assembly.Location;

                    return new ModelInfo
                    {
                        Name = x.Name,
                        ModelType = x,
                        Description = "",
                        Properties = x.GetProperties(),
                        SourceFilePath = x.FullName.Replace(".", "/") + ".cs",

                    };
                }).ToList();
            }

            return _registeredModels;
        }
    }
    public List<IBasicEntity> Palette { get; set; } = new List<IBasicEntity>();

    public ModelInfoService()
    {

    }

    public IEnumerable<ModelInfo> ModelList()
    {

        return RegisteredModels;

    }

    public ModelInfo GetModelInfo(object model)
    {
        Type type = model.GetType();
        var found = RegisteredModels.FirstOrDefault(x => x.ModelType == type);
        if (found == null) throw new ArgumentException("model not found in palette");
        return found;
    }

    public IEnumerable<IBasicEntity> GetPalette()
    {
        foreach (var type in RegisteredModelsTypes)
        {
            object handle = Activator.CreateInstance(type)!;
            IBasicEntity node = (IBasicEntity)handle;
            //node.X = 10;
            Palette.Add(node);

            //ObjectHandle handle = Activator.CreateInstance(type);
            //object instance = handle.Unwrap();
            //Palette.Add((Node)instance);

        }

        return Palette;
    }

    public static IEnumerable<Type> GetEnumerableOfType<T>(params object[] constructorArgs) where T : class
    {
        List<Type> objects = new List<Type>();
        foreach (Type type in
            Assembly.GetAssembly(typeof(T)).GetTypes()
            .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T))))
        {
            //objects.Add((T)Activator.CreateInstance(type, constructorArgs));
            //return typeof(T);
            objects.Add(type);
        }
        //objects.Sort();
        return objects;
    }

    public static IEnumerable<Type> GetInterfaceImplements(Type type, bool? publicOnly = true)
    {

        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p =>
                type.IsAssignableFrom(p)
                && (publicOnly == null || p.IsPublic == publicOnly)
                && p.IsClass
                && !p.IsAbstract
            );

        return types;
    }

    public static IEnumerable<Type> GetInterfaceImplements<T>(bool? publicOnly = null) where T : class
    {
        var type = typeof(T);
        return GetInterfaceImplements(type, publicOnly);
    }

    public List<GPageInfo> GetPages(Assembly assembly)
    {
        var type = typeof(ComponentBase);

        var types =
            //AppDomain.CurrentDomain.GetAssemblies()
            //.SelectMany(s => s.GetTypes())
            //Assembly.GetAssembly(typeof(Program)).GetTypes()
            //Assembly.GetAssembly(program).GetTypes()
            assembly.GetTypes()
            .Where(p =>
                type.IsAssignableFrom(p)
                && p.IsPublic
                && p.IsClass
                && !p.IsAbstract
            //&& p.Assembly ==
            );

        return types.Select(s =>
        {
            var kind = EComponentType.ComponentBase;
            List<string> urls = new();

            var attributes = s.GetCustomAttributes()?.ToList() ?? new List<Attribute>();

            if (s.IsSubclassOf(typeof(LayoutComponentBase))) kind = EComponentType.Layout;
            else if (attributes.Any(s => s is RouteAttribute))
            {
                kind = EComponentType.Page;
                foreach (var a in attributes.Where(x => x is RouteAttribute))
                {
                    if (a is RouteAttribute r)
                    {
                        urls.Add(r.Template);
                    }
                }
            }

            List<string> roles = new();

            foreach (var a in attributes.Where(s => s is AuthorizeAttribute))
            {
                var at = a as AuthorizeAttribute;
                if (at is null) continue;
                var list = at.Roles?.Split(',', StringSplitOptions.TrimEntries);
                if (list is not null) roles.AddRange(list);
            }

            string? pageDisplayAttributeName = null;
            //if (kind == EComponentType.Page)
            {
                var attr = attributes.FirstOrDefault(s => s is DisplayAttribute);
                if (attr is not null)
                {
                    DisplayAttribute dispAttr = (DisplayAttribute)attr;
                    pageDisplayAttributeName = dispAttr.Name;
                }
            }

            return new GPageInfo
            {
                Name = s.Name,
                Kind = kind,
                PageType = s,
                Attributes = attributes,
                Urls = urls,
                Roles = roles,
                DisplayAttributeName = pageDisplayAttributeName ?? SplitCamelCase(Regex.Replace(s.Name, "Page$", ""))

            };
        }).ToList();
    }

    public static string SplitCamelCase(string input)
    {
        return Regex.Replace(input, "([A-Z]+)", " $1", RegexOptions.Compiled).Trim();
    }

    public List<GPageInfo> GetPagesPageNonId(Assembly assembly)
    {
        return GetPages(assembly)
            .Where(s => s.Kind == EComponentType.Page)
            .Where(s => !s.Urls?.Any(x => x.Contains("/{")) ?? true)
            .ToList();
    }

    public string? GetFileNameFromPageClass(NavigationManager navigationManager, List<GPageInfo> pages)
    {
        //foreach (var p in _pages)
        //{
        //    if (p.Urls.Contains("/applicatione/edit"))
        //    {

        //        //Console.WriteLine("urls2=" + String.Join(';', p.Urls));
        //        string[] urls = p.Urls.Select(x => x.ToLower().TrimEnd('/').Trim()).ToArray();
        //        Console.WriteLine("urls2=" + String.Join(';', urls));

        //    }

        //    //if (p.Urls.Select(s => s.ToLower()).Contains("faq"))
        //    //{
        //    //    Console.WriteLine("urls=" + String.Join(';', p.Urls));

        //    //}
        //}

        var _pages = pages.Where(s => s.Kind == EComponentType.Page);

        string currentUrl = navigationManager.Uri.Replace(navigationManager.BaseUri, "/").Split("?")[0].TrimEnd('/').Trim();
        //Console.WriteLine(currentUrl);

        //currentUrl = Regex.Replace(currentUrl, @"(?im)[{(]?[0-9A-F]{8}[-]?(?:[0-9A-F]{4}[-]?){3}[0-9A-F]{12}[)}]?", "{ID:guid}");
        currentUrl = Regex.Replace(currentUrl, @"(?im)[{(]?[0-9A-F]{8}[-]?(?:[0-9A-F]{4}[-]?){3}[0-9A-F]{12}[)}]?", "{");

        currentUrl = currentUrl.ToLower();

        //Console.WriteLine("currentUrl=" + currentUrl);
        //GPageInfo page = null;
        //foreach (var p in _pages)
        //{
        //    string[] urls = p.Urls.Select(x => x.ToLower().TrimEnd('/').Trim()).ToArray();

        //    //if (!urls.Contains("applicatione")) continue;

        //    Console.WriteLine($">>t:{urls.Contains(currentUrl)} - {string.Join('|', urls)} ");

        //    if (urls.Contains(currentUrl))
        //    {
        //        page = p;
        //        break;
        //    }
        //}

        var page = _pages.FirstOrDefault(s => s.Urls?.Select(x => x.ToLower().TrimEnd('/').Trim()).Any(s => s == currentUrl) ?? false);

        if (page == null)
        {
            page = _pages.FirstOrDefault(s => s.Urls?.Select(x => x.ToLower().TrimEnd('/').Trim()).Any(s =>
            {
                //Console.WriteLine(s);
                return s.StartsWith(currentUrl);
            }) ?? false);
        }
        if (page == null)
        {
            Console.WriteLine("page not found: " + currentUrl);
            return null;
        }

        string ptype = page.PageType.Name;
        string fullclassname = page.PageType.FullName!;
        //string filename = fullclassname.Replace(nameof(AppFront) + ".", "").Replace(".", "/") + ".razor";
        string filename = fullclassname.Replace(".", "\\") + ".razor";

        Console.WriteLine("ptype=" + fullclassname);
        Console.WriteLine("filename=" + filename);

        return filename;
    }

    private readonly string assemblyNameReplaceVar = "_ASSEMBLY_NAME_";

    public string GetFileNameFromPageClass(Type pageType)
    {
        string fullclassname = pageType.FullName!;
        //string filename = fullclassname.Replace(nameof(AppFront) + ".", "").Replace(".", "/") + ".razor";

        var assemblyName = pageType.Assembly.GetName().Name!;
        fullclassname = fullclassname.Replace(assemblyName, assemblyNameReplaceVar);

        string filename = (fullclassname.Replace(".", "\\") + ".razor").Replace(assemblyNameReplaceVar, assemblyName);

        Console.WriteLine("ptype=" + fullclassname);
        Console.WriteLine("filename=" + filename);

        return filename;
    }

}

public class GPageInfo
{
    public required string Name { get; init; }
    public required Type PageType { get; init; }
    public required List<Attribute> Attributes { get; init; }
    public required EComponentType Kind { get; init; }
    public required List<string>? Urls { get; init; }
    public required List<string>? Roles { get; init; }
    public required string DisplayAttributeName { get; init; }
}

public enum EComponentType
{
    ComponentBase,
    Page,
    Layout,
    Other
}
