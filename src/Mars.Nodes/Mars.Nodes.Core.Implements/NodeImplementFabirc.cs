using System.Reflection;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Core.Implements;

public class NodeImplementFabirc
{
    Dictionary<Type, NodeImplementItem> _dict = [];
    public IReadOnlyDictionary<Type, NodeImplementItem> Dict { get { if (invalid) RefreshDict(); return _dict; } }

    bool invalid;

    HashSet<Assembly> assemblies = [];

    object _lock = new { };

    private void RefreshDict(bool force = false)
    {
        if (!invalid && !force) return;
        lock (_lock)
        {
            _dict.Clear();

            foreach (var assembly in assemblies)
            {

                var types = GetAllTypesImplementingOfInterface(typeof(INodeImplement<>), assembly).ToList();

                int count = types.Count();

                foreach (var type in types)
                {
                    _dict.Add(type.Key, type.Value);
                }
            }

            invalid = false;
        }
    }

    private Dictionary<Type, NodeImplementItem> GetAllTypesImplementingOfInterface(Type interfaceType, Assembly assembly)
    {
        if (!interfaceType.IsInterface)
            throw new ArgumentException($"{interfaceType} is not an interface");

        return assembly
            .GetTypes()
            .Where(t =>
                t.IsClass &&
                !t.IsAbstract &&
                t.GetInterfaces()
                    .Any(i =>
                        i.IsGenericType &&
                        i.GetGenericTypeDefinition() == interfaceType.GetGenericTypeDefinition()
                    )
            )
            .Select(t =>
            {
                // находим интерфейс с конкретным generic типом
                var implInterface = t.GetInterfaces()
                    .First(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == interfaceType.GetGenericTypeDefinition());

                var genericArg = implInterface.GetGenericArguments().Single();

                return new NodeImplementItem
                {
                    NodeBaseType = genericArg,
                    NodeImplementType = t
                };
            })
            .GroupBy(x => x.NodeBaseType)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public INodeImplement Create(INodeBasic node, IRED _RED)
    {
        Type instantiateType;

        if (node is ConfigNode) instantiateType = typeof(ConfigNodeImpl);
        else instantiateType = Dict[node.GetType()].NodeImplementType;

        try
        {
            //TODO: тут есть загвоздка. Он будет искать конструкторы для который требуется [node, _RED] иначе исключение
            var instance = (INodeImplement)ActivatorUtilities.CreateInstance(_RED.ServiceProvider, instantiateType, [node, _RED]);
            return instance;

        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException($"type '{instantiateType}' should require ctor with [({node.GetType().Name} node, IRED red, ...)]", ex);
        }
    }

    public void RegisterAssembly(Assembly assembly)
    {
        if (assemblies.Contains(assembly)) return;
        assemblies.Add(assembly);
        invalid = true;
    }

    //public  Type GetTypeByFullName(string typeFullname)
    //{
    //    Type type = typeof(Node).Assembly.GetType(typeFullname)!;
    //    return type;
    //}
}

public record NodeImplementItem
{
    public required Type NodeBaseType;
    public required Type NodeImplementType;
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
