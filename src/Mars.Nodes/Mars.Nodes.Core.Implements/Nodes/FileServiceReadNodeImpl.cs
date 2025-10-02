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
        if (Node.ByFileId && !Guid.TryParse(Node.StorageFileId, out var storageFileId))
            throw new NodeExecuteException(Node, $"Node.StorageFileId is not valid Guid '{Node.StorageFileId}'");
        else storageFileId = Guid.Empty;
        if (!Node.ByFileId && string.IsNullOrEmpty(Node.FilePath))
            throw new NodeExecuteException(Node, $"Node.FilePath is empty");

        var fs = RED.ServiceProvider.GetRequiredService<IFileStorage>();
        var fileService = RED.ServiceProvider.GetRequiredService<IMediaService>();

        var fileDetail = Node.ByFileId ? await fileService.GetDetail(storageFileId, parameters.CancellationToken) : null;
        var filepath = fileDetail?.FilePhysicalPath ?? Node.FilePath;

        if (Node.OutputMode == FileServiceReadNode.FileOutputMode.SingleBuffer)
        {
            var buffer = fs.Read(filepath);
            input.Payload = buffer;
            callback(input);
        }
        else if (Node.OutputMode == FileServiceReadNode.FileOutputMode.SingleString)
        {
            var text = fs.ReadAllText(filepath);
            input.Payload = text;
            callback(input);
        }
        else if (Node.OutputMode == FileServiceReadNode.FileOutputMode.MsgPerLine)
        {
            const int bufferSize = 2048;
            fs.Read(filepath, out var fileStream);
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
    }

}
