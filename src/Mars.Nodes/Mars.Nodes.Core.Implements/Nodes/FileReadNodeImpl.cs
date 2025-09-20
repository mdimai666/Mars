using Mars.Nodes.Core.Nodes;
using HandlebarsDotNet;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace Mars.Nodes.Core.Implements.Nodes;

public class FileReadNodeImpl : INodeImplement<FileReadNode>, INodeImplement
{
    public FileReadNodeImpl(FileReadNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public FileReadNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {

        if (Node.OutputMode == FileReadNode.FileOutputMode.SingleBuffer)
        {
            var buffer = File.ReadAllBytes(Node.Filename);
            input.Payload = buffer;
            callback(input);
        }
        else if (Node.OutputMode == FileReadNode.FileOutputMode.SingleString)
        {
            var text = File.ReadAllText(Node.Filename);
            input.Payload = text;
            callback(input);
        }
        else if (Node.OutputMode == FileReadNode.FileOutputMode.MsgPerLine)
        {
            const int bufferSize = 2048;
            using var fileStream = File.OpenRead(Node.Filename);
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, bufferSize);

            string? line;
            while ((line = streamReader.ReadLine()) != null)
            {
                input.Payload = line;
                callback(input);
            }
        }
        else
        {
            throw new NotImplementedException();
        }

        return Task.CompletedTask;
    }
}
