using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Shared.Dto;
using Microsoft.Extensions.DependencyInjection;
using static Mars.Nodes.Core.Nodes.FileWriteNode;

namespace Mars.Nodes.Core.Implements.Nodes;

public class FileServiceWriteNodeImpl : INodeImplement<FileServiceWriteNode>, INodeImplement
{
    public FileServiceWriteNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public FileServiceWriteNodeImpl(FileServiceWriteNode node, IRED red)
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
        var ct = parameters.CancellationToken;

        var fileDetail = Node.ByFileId
            ? await fileService.GetDetail(storageFileId, parameters.CancellationToken)
            : await fileService.GetFileByPath(Node.FilePath, parameters.CancellationToken);
        var filepath = fileDetail?.FilePhysicalPath ?? Node.FilePath;

        bool exist = fs.FileExists(filepath);
        var databaseRecordExist = fileDetail != null;

        if (Node.WriteMode == FileWriteMode.Delete)
        {
            if (exist)
            {
                if (databaseRecordExist)
                    await fileService.Delete(fileDetail.Id, ct);
                else
                    fs.Delete(filepath);
            }
            else
                throw new NodeExecuteException(Node, $"File '{filepath}' not found");
        }
        else
        {
            bool isString = input.Payload is string;
            bool isAppend = Node.WriteMode == FileWriteMode.Append;

            bool isBuffer = input.Payload is byte[];
            //using var ms = new MemoryStream(isBuffer ? )
            long fileSize;

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
                    //using var sw = File.AppendText(Node.Filename);
                    var text = fs.ReadAllText(filepath);
                    var content = Node.AddAsNewLine ? (text + payload + Environment.NewLine) : (text + payload);

                    fs.Write(filepath, content);
                    fileSize = content.Length;
                }
                else
                {
                    fs.Write(filepath, payload);
                    fileSize = payload.Length;
                }
            }
            else
            {
                if (isAppend)
                {
                    var original = fs.Read(filepath);

                    var buffer = (input.Payload as byte[])!;

                    using var ms = new MemoryStream();
                    ms.Write(original, 0, original.Length); // старые данные
                    ms.Write(buffer, 0, buffer.Length);

                    fs.Write(filepath, ms);
                    //await fileService.
                    fileSize = ms.Length;
                }
                else
                {
                    var content = (input.Payload as byte[])!;
                    fs.Write(filepath, content);
                    fileSize = content.Length;
                }
            }

            if (Node.CreateDatabaseRecord && !databaseRecordExist)
            {
                var fileRepository = RED.ServiceProvider.GetRequiredService<IFileRepository>();

                var originalFileNameWithExtShort = Path.GetFileName(filepath);
                var userId = input.Get<RequestUserInfo>()?.UserId ?? throw new NotFoundException("userInfo not found");
                var filepathFromUpload = filepath;
                var hostingInfo = RED.ServiceProvider.GetRequiredService<IOptionService>().FileHostingInfo();

                var createFileQuery = new CreateFileQuery
                {
                    Name = originalFileNameWithExtShort,
                    Size = (ulong)fileSize,
                    Meta = null,
                    UserId = userId,
                    FilePathFromUpload = filepathFromUpload,
                };
                var createdId = await fileRepository.Create(createFileQuery, hostingInfo, ct);

            }
        }

        callback(input);
    }
}
