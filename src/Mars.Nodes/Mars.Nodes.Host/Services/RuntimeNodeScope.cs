using Mars.Nodes.Core;
using Mars.Nodes.Core.Fields;
using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Host.Shared;
using Mars.Nodes.Host.Shared.HttpModule;
using Mars.Nodes.Host.Shared.Models;

namespace Mars.Nodes.Host.Services;

internal class RuntimeNodeScope : IRuntimeNodeScope
{
    public string NodeId { get; set; }
    public IFlowNodeImpl? Flow { get; }
    public List<HttpCatchRegister> HttpRegisterdCatchers => _runtime.HttpRegisterdCatchers;
    public IServiceProvider ServiceProvider { get; }

    public VariablesContextDictionary GlobalContext => _runtime.GlobalContext;
    public VariablesContextDictionary? FlowContext => Flow is null ? null : _runtime.FlowContexts[Flow!.Node.Id];

    INodeRuntime _runtime;

    public RuntimeNodeScope(string nodeId, IFlowNodeImpl? flow, INodeRuntime runtime, IServiceProvider serviceProvider)
    {
        NodeId = nodeId;
        Flow = flow;
        _runtime = runtime;
        ServiceProvider = serviceProvider;
    }

    public void DebugMsg(DebugMessage msg)
        => _runtime.DebugMsg(NodeId, msg);

    public void DebugMsg(Exception ex)
        => _runtime.DebugMsg(NodeId, ex);

    public void Status(NodeStatus nodeStatus)
        => _runtime.BroadcastStatus(NodeId, nodeStatus);

    public void RegisterHttpMiddleware(HttpCatchRegister mw)
        => _runtime.RegisterHttpMiddleware(mw);

    public IEnumerable<HttpCatchRegister> GetHttpCatchRegistersForMethod(string method)
        => _runtime.GetHttpCatchRegistersForMethod(method);

    public HttpClient GetHttpClient()
        => _runtime.GetHttpClient();

    public VarNodeVaribleDto? GetVarNodeVarible(string varName)
        => _runtime.GetVarNodeVarible(varName);

    public void SetVarNodeVarible(string varName, object? value)
        => _runtime.SetVarNodeVarible(varName, value);

    public IReadOnlyDictionary<string, VarNode> VarNodesDict => _runtime.VarNodesDict;
    public IReadOnlyDictionary<string, ConfigNode> ConfigNodesDict => _runtime.ConfigNodesDict;

    public InputConfig<TConfigNode> GetConfig<TConfigNode>(string id) where TConfigNode : ConfigNode
        => new() { Id = id, Value = _runtime.ConfigNodesDict.GetValueOrDefault(id ?? "") as TConfigNode };

    public InputConfig<TConfigNode> GetConfig<TConfigNode>(InputConfig<TConfigNode> config) where TConfigNode : ConfigNode
        => new() { Id = config.Id, Value = _runtime.ConfigNodesDict.GetValueOrDefault(config.Id ?? "") as TConfigNode };

    public void Done(ExecutionParameters parameters)
        => _runtime.Done(NodeId, parameters.JobGuid);
}
