using Mars.Nodes.Core.Implements.Utils;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class DirReadNodeImpl : INodeImplement<DirReadNode>, INodeImplement
{
    public DirReadNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public DirReadNodeImpl(DirReadNode node, IRED red)
    {
        Node = node;
        RED = red;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var fileListUtility = new FileListUtility();
        var files = fileListUtility.GetFiles(Node.DirPath,
                                            includeFilter: Node.Pattern,
                                            maxDepth: Node.MaxDepth,
                                            returnRelativePaths: Node.ReturnRelativePath);

        callback(input.Copy(files));
        return Task.CompletedTask;
    }
}
