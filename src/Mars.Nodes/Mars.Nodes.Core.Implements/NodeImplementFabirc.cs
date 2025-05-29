using System.Reflection;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements;

public static class NodeImplementFabirc
{
    public static Dictionary<Type, Type> dict = new();

    static bool invalide = true;

    static HashSet<Assembly> assemblies = new();

    static object _lock = new { };

    public static void RefreshDict(bool force = false)
    {
        if (!invalide && !force) return;
        lock (_lock)
        {
            dict.Clear();

            foreach (var assembly in assemblies)
            {

                var types = GetAllTypesImplementingOpenGenericType2(typeof(INodeImplement<>), assembly);
                //var types = typeof(NodeImplement<>).Assembly.GetTypes();

                int count = types.Count();

                foreach (Type type in types)
                {
                    ////var parent = type.BaseType; variant 1
                    var parent = type.GetInterfaces().FirstOrDefault(s => s.Name == typeof(INodeImplement<>).Name); //variant 2
                    Type nodeType = parent!.GenericTypeArguments.First();//Node Class
                    if (dict.ContainsKey(nodeType) == false)
                    {
                        dict.Add(nodeType, type);
                    }
                }
            }
        }
    }

    //https://stackoverflow.com/questions/8645430/get-all-types-implementing-specific-open-generic-type
    public static IEnumerable<Type> GetAllTypesImplementingOpenGenericType(Type openGenericType, Assembly assembly)
    {
        var types = from x in assembly.GetTypes()
                    let y = x.BaseType
                    where !x.IsAbstract && !x.IsInterface &&
                    y != null && y.IsGenericType &&
                    y.GetGenericTypeDefinition() == openGenericType
                    select x;
        return types;
    }
    public static IEnumerable<Type> GetAllTypesImplementingOpenGenericType2(Type openGenericType, Assembly assembly)
    {
        return from x in assembly.GetTypes()
               from z in x.GetInterfaces()
               let y = x.BaseType
               where
               !x.IsInterface &&
               ((y != null && y.IsGenericType &&
               openGenericType.IsAssignableFrom(y.GetGenericTypeDefinition())) ||
               (z.IsGenericType &&
               openGenericType.IsAssignableFrom(z.GetGenericTypeDefinition())))
               select x;
    }

    public static INodeImplement Create(INodeBasic node, IRED _RED)
    {
        Type instantiateType;

        if (node is ConfigNode) instantiateType = typeof(ConfigNodeImpl);
        else instantiateType = dict[node.GetType()];

        var ctor = instantiateType.GetConstructors().First();
        object instance;
        if (ctor.GetParameters().Length == 1)
            instance = Activator.CreateInstance(instantiateType, node)!;
        else
            instance = Activator.CreateInstance(instantiateType, node, _RED)!;
        INodeImplement nodeImpl = (INodeImplement)instance;

        //nodeImpl.Node = node;
        //nodeImpl = nodeImpl.Create(node);
        return nodeImpl;
        //return instance;
    }

    public static void RegisterAssembly(Assembly assembly)
    {
        if (assemblies.Contains(assembly)) return;
        assemblies.Add(assembly);
    }

    //public static Type GetTypeByFullName(string typeFullname)
    //{
    //    Type type = typeof(Node).Assembly.GetType(typeFullname)!;
    //    return type;
    //}
}

//public abstract class NodeImplement<TNode>: INodeImplement<TNode> where TNode : Node
//{
//    public TNode Node => node;

//    public string Id => Node.Id;

//    public IRED RED { get; set; }
//    protected TNode node;

//    //public virtual INodeImplement<TNode> Create(TNode node)
//    //{
//    //    this.node = node;
//    //    return (INodeImplement<TNode>)this;
//    //}

//    public NodeImplement(TNode node)
//    {
//        this.node = Node;
//    }

//        public Task Execute(NodeMsg input, ExecuteAction callback)


//#if asas

//    public virtual async Task Execute(InputMsg input, Action<object> callback, Action<Exception> Error, bool ErrorTolerance)
//    {

//        try
//        {
//            User user = input.Get<User>();

//            Applica app = input.Get("app") as Applica;




//            int age = 21;

//            input.Add("age", age);

//            int getAge = (int)input.Get("age");


//            if (user is not null)
//            {

//            }



//            input.Payload = user;

//            if (!ErrorTolerance) callback.Invoke(input);
//        }
//        catch (Exception e)
//        {
//            Error(e);
//        }
//        finally
//        {
//            if (ErrorTolerance)
//                callback.Invoke(input);
//        }

//    } 
//#endif
//}
