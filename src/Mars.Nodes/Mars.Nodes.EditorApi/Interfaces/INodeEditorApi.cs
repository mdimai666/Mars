using Mars.Nodes.Core;
using Microsoft.Extensions.Logging;

namespace Mars.Nodes.EditorApi.Interfaces;

public interface INodeEditorApi
{
    List<Type> RegisteredNodes { get; }
    List<Node> Palette { get; }
    IDictionary<string, Node> AllNodes { get; }
    INodeWorkspaceApi NodeWorkspace { get; }
    IEditorActionManager ActionManager { get; }

    void CallStateHasChanged();
    void SetNodes(IDictionary<string, Node> nodes);
    void SetNodes(IEnumerable<Node> nodes);
    void AddNodes(IEnumerable<Node> nodes);
    void DeleteNodes(IEnumerable<Node> nodes);

    void SetSelectContext(Type? type);
    void DeployClick();

    ILogger<T> CreateLogger<T>();
}
