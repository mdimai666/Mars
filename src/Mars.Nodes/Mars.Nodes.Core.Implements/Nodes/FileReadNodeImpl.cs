using System.Text;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Core.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Core.Implements.Nodes;

public class FileReadNodeImpl : INodeImplement<FileReadNode>, INodeImplement
{
    public FileReadNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public FileReadNodeImpl(FileReadNode node, IRED red)
    {
        Node = node;
        RED = red;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        if (Node.OutputMode == FileReadNode.FileOutputMode.SingleBuffer)
        {
            var buffer = File.ReadAllBytes(Node.FilePath);
            input.Payload = buffer;
            callback(input);
        }
        else if (Node.OutputMode == FileReadNode.FileOutputMode.SingleString)
        {
            var text = File.ReadAllText(Node.FilePath);
            input.Payload = text;
            callback(input);
        }
        else if (Node.OutputMode == FileReadNode.FileOutputMode.MsgPerLine)
        {
            const int bufferSize = 2048;
            using var fileStream = File.OpenRead(Node.FilePath);
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
