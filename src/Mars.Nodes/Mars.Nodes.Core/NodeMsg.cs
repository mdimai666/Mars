namespace Mars.Nodes.Core;

public class NodeMsg
{
    public object? Payload { get; set; }

    public Dictionary<string, object> Context { get; set; } = [];

    public T? Get<T>() where T : class
    {
        return Context.ContainsKey(typeof(T).Name) ? Context[typeof(T).Name] as T : default;
    }

    public object? Get(string name)
    {
        return Context.ContainsKey(name) ? Context[name] : null;
    }

    public void Add<T>(T obj)
    {
        Context.Add(typeof(T).Name, obj!);
    }

    public bool TryAdd<T>(T obj)
    {
        return Context.TryAdd(typeof(T).Name, obj!);
    }

    public void Add(string name, object obj)
    {
        Context.Add(name, obj);
    }

    public bool TryAdd(string name, object obj)
    {
        return Context.TryAdd(name, obj);
    }

    public void Set<T>(T obj)
    {
        Context[typeof(T).Name] = obj!;
    }

    public void Set(string name, object obj)
    {
        Context[name] = obj;
    }

    public Dictionary<string, object> AsFullDict()
    {
        return new Dictionary<string, object>(Context)
        {
            [nameof(Payload)] = Payload!
        };
    }

    public NodeMsg Copy(object? payload = null)
    {
        return new NodeMsg
        {
            Payload = payload ?? Payload,
            Context = new Dictionary<string, object>(Context)
        };
    }
}
