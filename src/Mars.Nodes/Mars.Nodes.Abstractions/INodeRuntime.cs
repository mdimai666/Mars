using Mars.Nodes.Abstractions.Hubs;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Abstractions.ExceptionModule;
using Mars.Nodes.Abstractions.HttpModule;
using Mars.Nodes.Abstractions.Models;

namespace Mars.Nodes.Abstractions;

public interface INodeRuntime
{
    IServiceProvider ServiceProvider { get; }
    BroadcastHub BroadcastHub { get; }
    INodeImplementFactory NodeImplementFactory { get; }
    List<HttpCatchRegister> HttpRegisterdCatchers { get; set; }
    VariablesContextDictionary GlobalContext { get; }
    Dictionary<string, VariablesContextDictionary> FlowContexts { get; }
    Dictionary<string, INodeImplement> Nodes { get; set; }
    IReadOnlyDictionary<string, VarNode> VarNodesDict { get; }
    IReadOnlyDictionary<string, ConfigNode> ConfigNodesDict { get; }
    IReadOnlyDictionary<string, Node> BasicNodesDict { get; }
    CompiledHttpRouteMatcher CompiledHttpRouteMatcher { get; }
    event NodeImplDoneEvent OnNodeImplDone;
    NodesErrorHandlerRegistry ErrorHandlerRegistry { get; }
    void AssignNodes(IReadOnlyCollection<Node> nodes, CancellationToken cancellationToken);

    IRuntimeNodeScope CreateContextForNode(Node node, IFlowNodeImpl? flow);
    IEnumerable<HttpCatchRegister> GetHttpCatchRegistersForMethod(string method);
    VarNodeVaribleDto? GetVarNodeVarible(string varName);
    void SetVarNodeVarible(string varName, object? value);
    void DebugMsg(string nodeId, DebugMessage msg);
    void DebugMsg(string nodeId, Exception ex);
    void BroadcastStatus(string nodeId, NodeStatus nodeStatus);
    void RegisterHttpMiddleware(HttpCatchRegister mw);
    HttpClient GetHttpClient();
    void Done(string nodeId, Guid jobGuid);

}

public delegate void NodeImplDoneEvent(string nodeId, Guid jobGuid);
