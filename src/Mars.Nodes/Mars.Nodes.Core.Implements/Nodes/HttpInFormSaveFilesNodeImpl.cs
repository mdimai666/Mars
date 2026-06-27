using Mars.Core.Extensions;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core.Implements.Utils;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Shared;
using Mars.Nodes.Host.Shared.HttpModule;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Core.Implements.Nodes;

public class HttpInFormSaveFilesNodeImpl : INodeImplement<HttpInFormSaveFilesNode>
{
    public HttpInFormSaveFilesNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public HttpInFormSaveFilesNodeImpl(HttpInFormSaveFilesNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;

    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var http = input.Get<HttpInNodeHttpRequestContext>();
        var request = http.HttpContext.Request;

        if (!request.HasFormContentType || request.Form.Files.None()) goto Out;

        var requestContext = RNS.ServiceProvider.GetRequiredService<IRequestContext>();
        var defaultUserId = async () => (await RNS.ServiceProvider.GetRequiredService<IUserService>().DefaultContentUserAsync(parameters.CancellationToken)).Id;
        Guid userId = requestContext.User?.Id ?? await defaultUserId();

        var optionsService = RNS.ServiceProvider.GetRequiredService<IOptionService>();

        if (Node.SaveInMediaFiles)
        {
            var mediaService = RNS.ServiceProvider.GetRequiredService<IMediaService>();
            List<Guid> writtenFileIds = [];
            foreach (var file in request.Form.Files)
            {
                var filepath = FilePathGenerator.Generate(Node.FilePathTemplate, file, file.Name);
                var fileName = Path.GetFileName(filepath);
                var fileDir = Path.GetDirectoryName(filepath)!;
                var writtenFileId = await mediaService.WriteUpload(fileName, fileDir, file.OpenReadStream(), userId, generateUniqueName: false, parameters.CancellationToken);
                writtenFileIds.Add(writtenFileId);
            }
            var fileRepository = RNS.ServiceProvider.GetRequiredService<IFileRepository>();
            var files = await fileRepository.ListAll(new() { Ids = writtenFileIds }, optionsService.FileHostingInfo(), parameters.CancellationToken);
            input.Payload = files;
            goto Out;
        }
        else if (Node.AllowSaveFileOutsideUploads)
        {
            List<string> fileNames = [];
            foreach (var file in request.Form.Files)
            {
                var filepath = FilePathGenerator.Generate(Node.FilePathTemplate, file, file.Name);
                var directory = Path.GetDirectoryName(filepath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                using (var stream = file.OpenReadStream())
                using (var fileStream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                {
                    await stream.CopyToAsync(fileStream);
                }
                fileNames.Add(filepath);
            }
            input.Payload = fileNames;
            goto Out;
        }
        else
        {
            List<string> fileNames = [];
            foreach (var file in request.Form.Files)
            {
                var filepath = FilePathGenerator.Generate(Node.FilePathTemplate, file, file.Name);
                var fs = RNS.ServiceProvider.GetRequiredService<IFileStorage>();
                await fs.WriteAsync(filepath, file.OpenReadStream(), parameters.CancellationToken);
                fileNames.Add(filepath);
            }
            input.Payload = fileNames;
            goto Out;
        }

    Out:
        callback(input);
    }

}
