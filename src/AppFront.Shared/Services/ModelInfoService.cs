using System.Reflection;
using Mars.Shared.Models.Interfaces;

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

}
