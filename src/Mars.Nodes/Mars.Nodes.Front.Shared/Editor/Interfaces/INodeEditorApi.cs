using System.Text.Json;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes;
using Microsoft.Extensions.Logging;

namespace Mars.Nodes.Front.Shared.Editor.Interfaces;

public interface INodeEditorApi
{
    IReadOnlyCollection<Type> RegisteredNodes { get; }
    IReadOnlyCollection<Node> Palette { get; }
    IDictionary<string, Node> AllNodes { get; }
    FlowNode? ActiveFlow { get; }
    INodeWorkspaceApi NodeWorkspace { get; }
    IEditorActionManager ActionManager { get; }
    JsonSerializerOptions NodesJsonSerializerOptions { get; }
    IReadOnlyDictionary<string, InlineFunctionNodeSchema> InlineFunctionNodeSchemas { get; }

    void SetNodes(IDictionary<string, Node> nodes);
    void SetNodes(IEnumerable<Node> nodes);
    void AddNodes(IEnumerable<Node> nodes);
    void DeleteNodes(IEnumerable<Node> nodes);
    void DeleteNodesAndWires(IEnumerable<Node> nodes, IEnumerable<NodeConnect> connects);

    void SetSelectContext(Type? type);
    void DeployClick();

    ILogger<T> CreateLogger<T>();
    void SaveNode(Node node, bool changed = true);
    void ChangeFlow(FlowNode flowNode);
    IReadOnlyDictionary<string, Node> GetFlowNodes(string flowId);
    void StartEditNode(Node node);
    void StartCreateNewConfigNode(AppendNewConfigNodeEvent appendNewConfigNodeEvent);
    void EnableHotkeys(bool enable);
    void RedrawWires();
    void AddDebugMessage(DebugMessage msg);
    void AddNodesAndWires(IEnumerable<Node> nodes, IEnumerable<NodeConnect> connects);
    void AddDebugMessage(string text);
}

public class AppendNewConfigNodeEvent
{
    public required Type ConfigNodeType { get; init; }
    public required Action<ConfigNode> ConfigNodeSetter { get; init; }
}
