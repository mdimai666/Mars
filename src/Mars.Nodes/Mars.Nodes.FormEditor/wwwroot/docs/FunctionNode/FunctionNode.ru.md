# FunctionNode

Исполняет произвольный код C#

## Example
```csharp
return 1+1;
```

### ScriptExecuteContext
```csharp
public class ScriptExecuteContext
{
    public string NodeId;
    public NodeMsg msg;
    public IRuntimeNodeScope RNS;
    public ExecuteAction callback;
    public FlowNodeImpl Flow => RNS.Flow;
    //public VariablesContextDictionary Flow.FlowContext;
    public VariablesContextDictionary GlobalContext => RNS.GlobalContext;

    public void Send(object msgOrPayload, int output = 0);
    public void Debug(object? obj);
}
```

### Context access
```csharp
using Microsoft.Extensions.DependencyInjection;

RNS.ServiceProvider.GetRequiredService<IRequestContext>();

msg.Payload = 1;
GlobalContext.x = 2;
Flow.FlowContext.y = 3;

```

### VarNode
```csharp
public VarNodeVaribleDto? GetVarNodeVarible(string varName);
public void SetVarNodeVarible(string varName, object? value);
public IReadOnlyDictionary<string, VarNode> VarNodesDict { get; }

RNS.GetVarNodeVarible("x")
```

### Services
- `IPostService`
- `IFileService`
- `IFileStorage`
- `IUserService`

### Json 
```csharp
string JsonNodeImpl.ToJsonString(object input, bool formatted = false)
T? JsonNodeImpl.ParseString<T>(string input)
JsonNode? JsonNodeImpl.ParseString(string input)
```
