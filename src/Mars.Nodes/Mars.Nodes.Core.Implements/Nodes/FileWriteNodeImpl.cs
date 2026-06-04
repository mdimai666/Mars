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
        var filePath = VariableSetNodeImpl.ReadFieldAsExpression(Node.FilePath, RED, input);

        switch (Node.WriteMode)
        {
            case FileWriteNode.FileWriteMode.Delete:
                if (!File.Exists(filePath))
                    throw new NodeExecuteException(Node, $"File '{filePath}' not found");
                File.Delete(filePath);
                break;

            case FileWriteNode.FileWriteMode.Append:
                EnsureDirectoryExists(filePath);
                AppendToFile(filePath, input.Payload);
                break;

            default: // Overwrite
                EnsureDirectoryExists(filePath);
                WriteToFile(filePath, input.Payload);
                break;
        }

        callback(input);
        return Task.CompletedTask;
    }

    private void AppendToFile(string filePath, object? payload)
    {
        switch (payload)
        {
            case null:
                AppendText(filePath, "null");
                break;

            case Stream stream:
                using (var fs = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                    stream.CopyTo(fs);
                break;

            case byte[] bytes:
                using (var fs = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                    fs.Write(bytes, 0, bytes.Length);
                break;

            case string text:
                AppendText(filePath, text);
                break;

            default:
                AppendText(filePath, JsonNodeImpl.ToJsonString(payload));
                break;
        }
    }

    private void WriteToFile(string filePath, object? payload)
    {
        switch (payload)
        {
            case null:
                File.WriteAllText(filePath, "null");
                break;

            case Stream stream:
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                    stream.CopyTo(fs);
                break;

            case byte[] bytes:
                File.WriteAllBytes(filePath, bytes);
                break;

            case string text:
                File.WriteAllText(filePath, text);
                break;

            default:
                File.WriteAllText(filePath, JsonNodeImpl.ToJsonString(payload));
                break;
        }
    }

    private void AppendText(string filePath, string text)
    {
        using var sw = File.AppendText(filePath);
        if (Node.AddAsNewLine)
            sw.WriteLine(text);
        else
            sw.Write(text);
    }

    private void EnsureDirectoryExists(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
