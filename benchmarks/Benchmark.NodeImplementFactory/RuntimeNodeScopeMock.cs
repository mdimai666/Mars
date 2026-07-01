using Mars.Nodes.Core;
using Mars.Nodes.Core.Fields;
using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Host.Shared;
using Mars.Nodes.Host.Shared.HttpModule;
using Mars.Nodes.Host.Shared.Models;

public class RuntimeNodeScopeMock : IRuntimeNodeScope
{
    public IFlowNodeImpl? Flow { get; }
    public List<HttpCatchRegister> HttpRegisterdCatchers { get; } = [];
    public IServiceProvider ServiceProvider { get; }
    public VariablesContextDictionary GlobalContext { get; } = new();
    public VariablesContextDictionary? FlowContext { get; } = new();
    public IReadOnlyDictionary<string, VarNode> VarNodesDict { get; } = new Dictionary<string, VarNode>();
    public IReadOnlyDictionary<string, ConfigNode> ConfigNodesDict { get; } = new Dictionary<string, ConfigNode>();

    public RuntimeNodeScopeMock(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public void DebugMsg(DebugMessage msg)
    {
        throw new NotImplementedException();
    }

    public void DebugMsg(Exception ex)
    {
        throw new NotImplementedException();
    }

    public void Done(ExecutionParameters parameters)
    {
        throw new NotImplementedException();
    }

    public InputConfig<TConfigNode> GetConfig<TConfigNode>(string id) where TConfigNode : ConfigNode
    {
        throw new NotImplementedException();
    }

    public InputConfig<TConfigNode> GetConfig<TConfigNode>(InputConfig<TConfigNode> config) where TConfigNode : ConfigNode
    {
        throw new NotImplementedException();
    }

    public IEnumerable<HttpCatchRegister> GetHttpCatchRegistersForMethod(string method)
    {
        throw new NotImplementedException();
    }

    public HttpClient GetHttpClient()
    {
        throw new NotImplementedException();
    }

    public VarNodeVaribleDto? GetVarNodeVarible(string varName)
    {
        throw new NotImplementedException();
    }

    public void RegisterHttpMiddleware(HttpCatchRegister mw)
    {
        throw new NotImplementedException();
    }

    public void SetVarNodeVarible(string varName, object? value)
    {
        throw new NotImplementedException();
    }

    public void Status(NodeStatus nodeStatus)
    {
        throw new NotImplementedException();
    }
}
