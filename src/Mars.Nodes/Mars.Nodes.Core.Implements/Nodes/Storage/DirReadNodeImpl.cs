using Mars.Nodes.Core.Implements.Utils;
using Mars.Nodes.Core.Nodes.Storage;
using Mars.Nodes.Host.Shared;

namespace Mars.Nodes.Core.Implements.Nodes.Storage;

public class DirReadNodeImpl : INodeImplement<DirReadNode>
{
    public DirReadNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public DirReadNodeImpl(DirReadNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var fileListUtility = new FileListUtility();
        var files = fileListUtility.GetFiles(Node.DirPath,
                                            includeFilter: Node.Pattern,
                                            maxDepth: Node.MaxDepth,
                                            returnRelativePaths: Node.ReturnRelativePath);

        input.Payload = files;
        callback(input);
        return Task.CompletedTask;
    }
}
