using System.Text.Json;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class FileWriteNodeImpl : INodeImplement<FileWriteNode>, INodeImplement
{
    public FileWriteNodeImpl(FileWriteNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public FileWriteNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public Task Execute(NodeMsg input, ExecuteAction callback, Action<Exception> Error)
    {
        bool exist = File.Exists(Node.Filename);

        if (Node.WriteMode == FileWriteNode.FileWriteMode.Delete)
        {
            if (exist)
            {
                File.Delete(Node.Filename);
            }
        }
        else
        {
            //const int bufferSize = 2048;
            bool isString = input.Payload is string;
            bool isAppend = Node.WriteMode == FileWriteNode.FileWriteMode.Append;

            //using var fileStream = File.OpenWrite(Node.Filename);
            //using var streamReader = new StreamWriter(fileStream, Encoding.UTF8, bufferSize);

            //streamReader.w
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
                    using var sw = File.AppendText(Node.Filename);
                    if (Node.AddAsNewLine)
                        sw.WriteLine(payload);
                    else
                        sw.Write(payload);
                }
                else
                {
                    File.WriteAllText(Node.Filename, payload);
                }
            }
            else
            {

                if (isAppend)
                {
                    using var sw = File.OpenWrite(Node.Filename);

                    var buffer = input.Payload as byte[];

                    //byte[] bytes = new byte[buffer.Length];
                    sw.Write(buffer);
                }
                else
                {
                    File.WriteAllBytes(Node.Filename, (input.Payload as byte[])!);
                }
            }


        }

        callback(input);

        return Task.CompletedTask;
    }
}

