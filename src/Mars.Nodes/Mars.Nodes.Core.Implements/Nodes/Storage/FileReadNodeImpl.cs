using System.Text;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Core.Nodes.Storage;
using Mars.Nodes.Host.Shared;

namespace Mars.Nodes.Core.Implements.Nodes.Storage;

public class FileReadNodeImpl : INodeImplement<FileReadNode>
{
    public FileReadNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public FileReadNodeImpl(FileReadNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var ct = parameters.CancellationToken;
        var filepath = Node.FilePath;

        // 1. Валидация пути
        if (string.IsNullOrWhiteSpace(filepath))
            throw new NodeExecuteException(Node, "FilePath is empty");

        // 2. Проверка существования файла
        if (!File.Exists(filepath))
            throw new NodeExecuteException(Node, $"File '{filepath}' not found");

        // 3. Чтение в зависимости от режима
        switch (Node.OutputMode)
        {
            case FileReadNode.FileOutputMode.SingleBuffer:
                await ReadAsBufferAsync(filepath, input, callback, ct);
                break;

            case FileReadNode.FileOutputMode.SingleString:
                await ReadAsStringAsync(filepath, input, callback, ct);
                break;

            case FileReadNode.FileOutputMode.MsgPerLine:
                await ReadLineByLineAsync(filepath, input, callback, ct);
                break;

            //case FileReadNode.FileOutputMode.SingleStream:
            //    ReadAsStream(filepath, input, callback);
            //    break;

            default:
                throw new NodeExecuteException(Node, $"Unsupported output mode: {Node.OutputMode}");
        }
    }

    private static async Task ReadAsBufferAsync(
        string filepath,
        NodeMsg input,
        ExecuteAction callback,
        CancellationToken ct)
    {
        var buffer = await File.ReadAllBytesAsync(filepath, ct);
        input.Payload = buffer;
        callback(input);
    }

    private static async Task ReadAsStringAsync(
        string filepath,
        NodeMsg input,
        ExecuteAction callback,
        CancellationToken ct)
    {
        var text = await File.ReadAllTextAsync(filepath, ct);
        input.Payload = text;
        callback(input);
    }

    private static async Task ReadLineByLineAsync(
        string filepath,
        NodeMsg input,
        ExecuteAction callback,
        CancellationToken ct)
    {
        const int bufferSize = 8192;

        using var fileStream = new FileStream(
            filepath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize,
            useAsync: true);

        using var streamReader = new StreamReader(
            fileStream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: bufferSize);

        string? line;
        while ((line = await streamReader.ReadLineAsync(ct)) != null)
        {
            ct.ThrowIfCancellationRequested();

            input.Payload = line;
            callback(input);
        }
    }

    //private static void ReadAsStream(string filepath, NodeMsg input, ExecuteAction callback)
    //{
    //    // useAsync: true важен, чтобы следующие ноды могли использовать асинхронные методы (CopyToAsync и т.д.)
    //    var stream = new FileStream(
    //        filepath,
    //        FileMode.Open,
    //        FileAccess.Read,
    //        FileShare.Read,
    //        bufferSize: 8192,
    //        useAsync: true);

    //    input.Payload = stream;
    //    callback(input);
    //}
}
