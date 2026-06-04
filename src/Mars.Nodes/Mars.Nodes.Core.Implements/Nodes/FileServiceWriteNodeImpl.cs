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
        var ct = parameters.CancellationToken;

        // 1. Валидация и получение физического пути
        var (physicalPath, fileDetail) = await ResolveFilePathAsync(ct);

        var fs = RED.ServiceProvider.GetRequiredService<IFileStorage>();
        var fileService = RED.ServiceProvider.GetRequiredService<IMediaService>();

        // 2. Выполнение операции
        if (Node.WriteMode == FileWriteMode.Delete)
        {
            await DeleteFileAsync(fs, fileService, physicalPath, fileDetail, ct);
        }
        else
        {
            long fileSize = await WritePayloadAsync(fs, physicalPath, input.Payload, ct);
            await EnsureDatabaseRecordAsync(physicalPath, fileDetail, fileSize, input, ct);
        }

        callback(input);
    }

    private async Task<(string PhysicalPath, FileDetail? FileDetail)> ResolveFilePathAsync(CancellationToken ct)
    {
        var fileService = RED.ServiceProvider.GetRequiredService<IMediaService>();

        if (Node.ByFileId)
        {
            if (!Guid.TryParse(Node.StorageFileId, out var fileId))
                throw new NodeExecuteException(Node, $"StorageFileId is not a valid Guid: '{Node.StorageFileId}'");

            var detail = await fileService.GetDetail(fileId, ct);
            var path = detail?.FilePhysicalPath
                ?? throw new NodeExecuteException(Node, $"File with ID {fileId} not found in database");

            return (path, detail);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(Node.FilePath))
                throw new NodeExecuteException(Node, "FilePath is empty");

            var detail = await fileService.GetFileByPath(Node.FilePath, ct);
            var path = detail?.FilePhysicalPath ?? Node.FilePath;

            return (path, detail);
        }
    }

    private async Task DeleteFileAsync(IFileStorage fs, IMediaService fileService, string path, FileDetail? detail, CancellationToken ct)
    {
        bool physicalExists = fs.FileExists(path);
        bool dbExists = detail != null;

        if (!physicalExists && !dbExists)
            throw new NodeExecuteException(Node, $"File '{path}' not found");

        if (dbExists)
        {
            // Предполагаем, что fileService.Delete удаляет и физический файл, и запись в БД
            await fileService.Delete(detail.Id, ct);
        }
        else if (physicalExists)
        {
            fs.Delete(path);
        }
    }

    private async Task<long> WritePayloadAsync(IFileStorage fs, string path, object? payload, CancellationToken ct)
    {
        bool isAppend = Node.WriteMode == FileWriteMode.Append;

        switch (payload)
        {
            case null:
                return WriteText(fs, path, "null", isAppend);
            case string text:
                return WriteText(fs, path, text, isAppend);
            case byte[] bytes:
                return WriteBinary(fs, path, bytes, isAppend);
            case Stream stream:
                return await WriteBinaryStreamAsync(fs, path, stream, isAppend);
            default:
                // Объект сериализуем в JSON
                string json = JsonNodeImpl.ToJsonString(payload);
                return WriteText(fs, path, json, isAppend);
        }
    }

    private long WriteText(IFileStorage fs, string path, string text, bool isAppend)
    {
        if (isAppend)
        {
            var existing = fs.FileExists(path) ? fs.ReadAllText(path) : string.Empty;

            // Умная склейка: если файл не пуст и не заканчивается переносом строки, добавляем его
            string content;
            if (Node.AddAsNewLine)
            {
                if (string.IsNullOrEmpty(existing))
                    content = text + Environment.NewLine;
                else
                {
                    var trimmed = existing.TrimEnd('\r', '\n');
                    content = trimmed + Environment.NewLine + text + Environment.NewLine;
                }
            }
            else
            {
                content = existing + text;
            }

            fs.Write(path, content);
            return GetByteCount(content);
        }
        else
        {
            fs.Write(path, text);
            return GetByteCount(text);
        }
    }

    private long WriteBinary(IFileStorage fs, string path, byte[] buffer, bool isAppend)
    {
        if (isAppend)
        {
            var original = fs.FileExists(path) ? fs.Read(path) : Array.Empty<byte>();
            using var ms = new MemoryStream(original.Length + buffer.Length);
            ms.Write(original, 0, original.Length);
            ms.Write(buffer, 0, buffer.Length);
            ms.Position = 0;

            fs.Write(path, ms);
            return ms.Length;
        }
        else
        {
            fs.Write(path, buffer);
            return buffer.Length;
        }
    }

    private async Task<long> WriteBinaryStreamAsync(IFileStorage fs, string path, Stream stream, bool isAppend)
    {
        if (isAppend)
        {
            var original = fs.FileExists(path) ? fs.Read(path) : Array.Empty<byte>();
            using var ms = new MemoryStream();
            ms.Write(original, 0, original.Length);
            await stream.CopyToAsync(ms);
            ms.Position = 0;

            fs.Write(path, ms);
            return ms.Length;
        }
        else
        {
            fs.Write(path, stream);
            return stream.Length;
        }
    }

    private long GetByteCount(string text)
    {
        // Важно: считаем количество байт в UTF-8, а не количество символов (char.Length)
        return System.Text.Encoding.UTF8.GetByteCount(text);
    }

    private async Task EnsureDatabaseRecordAsync(string physicalPath, FileDetail? fileDetail, long fileSize, NodeMsg input, CancellationToken ct)
    {
        if (!Node.CreateDatabaseRecord || fileDetail != null)
            return;

        var fileRepository = RED.ServiceProvider.GetRequiredService<IFileRepository>();
        var hostingInfo = RED.ServiceProvider.GetRequiredService<IOptionService>().FileHostingInfo();

        var fileName = Path.GetFileName(physicalPath);
        var userId = input.Get<RequestUserInfo>()?.UserId ?? throw new NotFoundException("userInfo not found");

        var query = new CreateFileQuery
        {
            Name = fileName,
            Size = (ulong)fileSize,
            Meta = null,
            UserId = userId,
            FilePathFromUpload = physicalPath,
        };

        await fileRepository.Create(query, hostingInfo, ct);
    }
}
