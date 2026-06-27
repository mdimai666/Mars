using Mars.Nodes.Core;
using Mars.Nodes.Core.Fields;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Shared.HttpModule;
using Mars.Nodes.Host.Shared.Models;

namespace Mars.Nodes.Host.Shared;

public interface IRuntimeNodeScope
{
    IFlowNodeImpl? Flow { get; }

    void Status(NodeStatus nodeStatus);
    void DebugMsg(DebugMessage msg);
    void DebugMsg(Exception ex);

    void RegisterHttpMiddleware(HttpCatchRegister mw);

    List<HttpCatchRegister> HttpRegisterdCatchers { get; }
    IEnumerable<HttpCatchRegister> GetHttpCatchRegistersForMethod(string method);

    HttpClient GetHttpClient();

    IServiceProvider ServiceProvider { get; }
    VariablesContextDictionary GlobalContext { get; }
    VariablesContextDictionary? FlowContext { get; }

    VarNodeVaribleDto? GetVarNodeVarible(string varName);
    void SetVarNodeVarible(string varName, object? value);

    IReadOnlyDictionary<string, VarNode> VarNodesDict { get; }

    IReadOnlyDictionary<string, ConfigNode> ConfigNodesDict { get; }

    InputConfig<TConfigNode> GetConfig<TConfigNode>(string id) where TConfigNode : ConfigNode;
    InputConfig<TConfigNode> GetConfig<TConfigNode>(InputConfig<TConfigNode> config) where TConfigNode : ConfigNode;

    void Done(ExecutionParameters parameters);
}
