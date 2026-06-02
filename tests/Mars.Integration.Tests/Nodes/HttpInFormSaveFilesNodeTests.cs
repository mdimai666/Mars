using System.Text;
using FluentAssertions;
using Flurl.Http;
using Mars.Core.Extensions;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Nodes;

public class HttpInFormSaveFilesNodeTests : ApplicationTests, IDisposable
{
    const string _apiUrl = "/nodes/savefile";
    private readonly INodeService _nodeService;
    private List<string> _toRemoveFiles = [];

    public HttpInFormSaveFilesNodeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _nodeService = AppFixture.ServiceProvider.GetRequiredService<INodeService>();
    }

    void SetupNodesForUpload(HttpInFormSaveFilesNode node)
    {
        var nodes = NodesWorkflowBuilder.Create()
                                        .AddNext(new HttpInNode { Method = "POST", UrlPattern = _apiUrl, AllowMultipart = true, MaxFileSize = "10mb" })
                                        .AddNext(node)
                                        .AddNext(new HttpResponseNode())
                                        .BuildWithFlowNode();
        _nodeService.Deploy(nodes);
    }

    [IntegrationFact]
    public async Task Execute_SaveInMediaFiles_ShouldSaveFile()
    {
        //Arrange
        _ = nameof(HttpInNodeImpl.Execute);
        _ = nameof(HttpInFormSaveFilesNodeImpl.Execute);
        var client = AppFixture.GetClient();
        var fileStorage = AppFixture.ServiceProvider.GetRequiredService<IFileStorage>();
        var mediaService = AppFixture.ServiceProvider.GetRequiredService<IMediaService>();
        var optionService = AppFixture.ServiceProvider.GetRequiredService<IOptionService>();

        var fileContent = "TEST-text";
        var fileName = "file1.txt";
        SetupNodesForUpload(new()
        {
            FilePathTemplate = "media/{yyyy}/q/{field_name}/{file_name}",
            SaveInMediaFiles = true,
            AllowSaveFileOutsideUploads = false,
        });

        //Act
        var result = await client.Request(_apiUrl)
                                .PostMultipartAsync(mp => mp
                                    .AddFile("filefield", GenerateStreamFromString(fileContent), fileName)
                                )
                                .ReceiveJson<FileSummary[]>();

        //Assert
        result.Should().HaveCount(1);
        var fullPath = result[0].UrlRelative.TrimSubstringStart("/upload/");
        _toRemoveFiles.Add(fullPath);
        result[0].Name.Should().Be(fileName);
        result[0].UrlRelative.Should().Be($"/upload/media/{DateTime.Now.Year}/q/filefield/{fileName}");
        // fileStorage может быть виртуальным.
        fileStorage.FileExists(fullPath).Should().BeTrue();
        (await mediaService.GetDetail(result[0].Id, default)).Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task Execute_AllowSaveFileOutsideUploads_ShouldSaveFile()
    {
        //Arrange
        _ = nameof(HttpInNodeImpl.Execute);
        _ = nameof(HttpInFormSaveFilesNodeImpl.Execute);
        var client = AppFixture.GetClient();

        var fileContent = "TEST-text";
        var fileName = "file1.txt";
        var tempFilePath = Path.GetTempFileName();
        _toRemoveFiles.Add(tempFilePath);
        SetupNodesForUpload(new()
        {
            FilePathTemplate = tempFilePath,
            SaveInMediaFiles = false,
            AllowSaveFileOutsideUploads = true,
        });

        //Act
        var result = await client.Request(_apiUrl)
                                .PostMultipartAsync(mp => mp
                                    .AddFile("filefield", GenerateStreamFromString(fileContent), fileName)
                                )
                                .ReceiveJson<string[]>();

        //Assert
        result.Should().HaveCount(1);
        File.Exists(tempFilePath).Should().BeTrue();
    }

    [IntegrationFact]
    public async Task Execute_IFileStorage_ShouldSaveFile()
    {
        //Arrange
        _ = nameof(HttpInNodeImpl.Execute);
        _ = nameof(HttpInFormSaveFilesNodeImpl.Execute);
        var client = AppFixture.GetClient();
        var optionService = AppFixture.ServiceProvider.GetRequiredService<IOptionService>();
        var hostingInfo = optionService.FileHostingInfo();

        var fileContent = "TEST-text";
        var fileName = "file1.txt";
        SetupNodesForUpload(new()
        {
            FilePathTemplate = "media/{yyyy}/q/{field_name}/{file_name}",
            SaveInMediaFiles = false,
            AllowSaveFileOutsideUploads = false,
        });

        //Act
        var result = await client.Request(_apiUrl)
                                .PostMultipartAsync(mp => mp
                                    .AddFile("filefield", GenerateStreamFromString(fileContent), fileName)
                                )
                                .ReceiveJson<string[]>();

        //Assert
        result.Should().HaveCount(1);
        var fullPath = hostingInfo.FileAbsolutePath(result[0]);
        _toRemoveFiles.Add(fullPath);
        File.Exists(fullPath).Should().BeTrue();
    }

    private MemoryStream GenerateStreamFromString(string value)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
    }

    public void Dispose()
    {
        foreach (var file in _toRemoveFiles)
        {
            if (File.Exists(file)) File.Delete(file);
        }
    }
}
