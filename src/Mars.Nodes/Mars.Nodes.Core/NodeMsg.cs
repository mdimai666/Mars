namespace Mars.Nodes.Core;

public class NodeMsg
{
    public object? Payload { get; set; }

    Dictionary<string, object> Context { get; set; } = [];

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
        if (Context.ContainsKey(typeof(T).Name))
        {
            Context[typeof(T).Name] = obj!;
        }
        else
        {
            Context.Add(typeof(T).Name, obj!);
        }
    }

    public Dictionary<string, object> AsFullDict()
    {
        return new(Context)
        {
            { nameof(Payload), Payload! }
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

public record ExecutionParameters(
    Guid TaskId,
    Guid JobGuid,
    int InputPort = 0,
    CancellationToken CancellationToken = default,
    bool IsDebugMode = false
//TimeSpan Timeout
);
