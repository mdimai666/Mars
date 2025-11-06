using System.Text.Json;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes;
using Microsoft.Extensions.Logging;

namespace Mars.Nodes.EditorApi.Interfaces;

public interface INodeEditorApi
{
    IReadOnlyCollection<Type> RegisteredNodes { get; }
    IReadOnlyCollection<Node> Palette { get; }

    IDictionary<string, Node> AllNodes { get; }
    INodeWorkspaceApi NodeWorkspace { get; }
    IEditorActionManager ActionManager { get; }
    JsonSerializerOptions NodesJsonSerializerOptions { get; }

    void CallStateHasChanged();
    void SetNodes(IDictionary<string, Node> nodes);
    void SetNodes(IEnumerable<Node> nodes);
    void AddNodes(IEnumerable<Node> nodes);
    void DeleteNodes(IEnumerable<Node> nodes);

    void SetSelectContext(Type? type);
    void DeployClick();

    ILogger<T> CreateLogger<T>();
    void SaveNode(Node node, bool changed = true);
    void ChangeFlow(FlowNode flowNode);
    IReadOnlyDictionary<string, Node> GetFlowNodes(string flowId);
    void StartEditNode(Node node);
    void StartCreateNewConfigNode(AppendNewConfigNodeEvent appendNewConfigNodeEvent);
}

public class AppendNewConfigNodeEvent
{
    public required Type ConfigNodeType { get; init; }
    public required Action<ConfigNode> ConfigNodeSetter { get; init; }
}
