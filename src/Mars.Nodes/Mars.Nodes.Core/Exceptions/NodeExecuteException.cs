namespace Mars.Nodes.Core.Exceptions;

public class NodeExecuteException : Exception
{
    public string NodeId { get; }
    public Type NodeType { get; }

    public NodeExecuteException(string nodeId, Type nodeType, string message) : base(message)
    {
        NodeId = nodeId;
        NodeType = nodeType;
    }

    public NodeExecuteException(Node node, string message) : base(message)
    {
        NodeId = node.Id;
        NodeType = node.GetType();
    }
}
