using System.Text;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Core.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Core.Implements.Nodes;

public class FileServiceReadNodeImpl : INodeImplement<FileServiceReadNode>, INodeImplement
{
    public FileServiceReadNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public FileServiceReadNodeImpl(FileServiceReadNode node, IRED red)
    {
        Node = node;
        RED = red;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var ct = parameters.CancellationToken;

        // 1. Получаем физический путь к файлу
        var filepath = await ResolveFilePathAsync(ct);

        var fs = RED.ServiceProvider.GetRequiredService<IFileStorage>();

        // 2. Проверяем существование файла
        if (!fs.FileExists(filepath))
            throw new NodeExecuteException(Node, $"File '{filepath}' not found");

        // 3. Читаем файл в зависимости от режима
        switch (Node.OutputMode)
        {
            case FileServiceReadNode.FileOutputMode.SingleBuffer:
                ReadAsBuffer(fs, filepath, input, callback);
                break;

            case FileServiceReadNode.FileOutputMode.SingleString:
                ReadAsString(fs, filepath, input, callback);
                break;

            case FileServiceReadNode.FileOutputMode.MsgPerLine:
                await ReadLineByLineAsync(fs, filepath, input, callback, ct);
                break;

            //case FileServiceReadNode.FileOutputMode.SingleStream:
            //    ReadAsStream(fs, filepath, input, callback);
            //    break;

            default:
                throw new NodeExecuteException(Node, $"Unsupported output mode: {Node.OutputMode}");
        }
    }

    private async Task<string> ResolveFilePathAsync(CancellationToken ct)
    {
        var fileService = RED.ServiceProvider.GetRequiredService<IMediaService>();

        if (Node.ByFileId)
        {
            if (!Guid.TryParse(Node.StorageFileId, out var fileId))
                throw new NodeExecuteException(Node, $"StorageFileId is not a valid Guid: '{Node.StorageFileId}'");

            var detail = await fileService.GetDetail(fileId, ct);
            return detail?.FilePhysicalPath
                ?? throw new NodeExecuteException(Node, $"File with ID {fileId} not found in database");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(Node.FilePath))
                throw new NodeExecuteException(Node, "FilePath is empty");

            return Node.FilePath;
        }
    }

    private void ReadAsBuffer(IFileStorage fs, string filepath, NodeMsg input, ExecuteAction callback)
    {
        var buffer = fs.Read(filepath);
        input.Payload = buffer;
        callback(input);
    }

    private void ReadAsString(IFileStorage fs, string filepath, NodeMsg input, ExecuteAction callback)
    {
        var text = fs.ReadAllText(filepath);
        input.Payload = text;
        callback(input);
    }

    private async Task ReadLineByLineAsync(
        IFileStorage fs,
        string filepath,
        NodeMsg input,
        ExecuteAction callback,
        CancellationToken ct)
    {
        const int bufferSize = 8192;

        fs.Read(filepath, out var fileStream);
        using (fileStream)
        using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize))
        {
            string? line;
            while ((line = await streamReader.ReadLineAsync(ct)) != null)
            {
                ct.ThrowIfCancellationRequested();

                input.Payload = line;
                callback(input);
            }
        }
    }

    //private void ReadAsStream(IFileStorage fs, string filepath, NodeMsg input, ExecuteAction callback)
    //{
    //    // Вариант А: Если в IFileStorage есть метод OpenRead (рекомендуемый)
    //    // var stream = fs.OpenRead(filepath);

    //    // Вариант Б: Если используется ваш текущий API с out параметром
    //    fs.Read(filepath, out var stream);

    //    input.Payload = stream;
    //    callback(input);
    //}

}
