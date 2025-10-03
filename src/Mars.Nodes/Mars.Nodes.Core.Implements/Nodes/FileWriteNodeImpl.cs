using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class FileWriteNodeImpl : INodeImplement<FileWriteNode>, INodeImplement
{
    public FileWriteNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public FileWriteNodeImpl(FileWriteNode node, IRED red)
    {
        Node = node;
        RED = red;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        bool exist = File.Exists(Node.FilePath);

        if (Node.WriteMode == FileWriteNode.FileWriteMode.Delete)
        {
            if (exist)
                File.Delete(Node.FilePath);
            else
                throw new NodeExecuteException(Node, $"File '{Node.FilePath}' not found");
        }
        else
        {
            bool isString = input.Payload is string;
            bool isAppend = Node.WriteMode == FileWriteNode.FileWriteMode.Append;

            bool isBuffer = input.Payload is byte[];

            if (!isBuffer)
            {
                string payload;

                if (isString)
                {
                    payload = (input.Payload is null) ? "null" : (input.Payload as string)!;
                }
                else
                {
                    string? json = JsonNodeImpl.ToJsonString(input.Payload!);
                    payload = json;
                }

                if (isAppend)
                {
                    using var sw = File.AppendText(Node.FilePath);
                    if (Node.AddAsNewLine)
                        sw.WriteLine(payload);
                    else
                        sw.Write(payload);
                }
                else
                {
                    File.WriteAllText(Node.FilePath, payload);
                }
            }
            else
            {

                if (isAppend)
                {
                    using var sw = File.OpenWrite(Node.FilePath);

                    var buffer = input.Payload as byte[];

                    //byte[] bytes = new byte[buffer.Length];
                    sw.Write(buffer);
                }
                else
                {
                    File.WriteAllBytes(Node.FilePath, (input.Payload as byte[])!);
                }
            }

        }

        callback(input);

        return Task.CompletedTask;
    }
}
