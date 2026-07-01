using System.Text;
using FluentAssertions;
using Flurl.Http;
using Mars.Core.Extensions;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes.Network;
using Mars.Nodes.Core.Nodes.Network;
using Mars.Nodes.Core.Utils;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Nodes.HttpInNodeTests;

public class HttpInNodeMultipartFormTests : ApplicationTests
{
    const string _apiUrl = "";
    private readonly INodeService _nodeService;

    public HttpInNodeMultipartFormTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _nodeService = AppFixture.ServiceProvider.GetRequiredService<INodeService>();
    }

    void SetupNodesForUpload(Action<NodeMsg>? callback, bool allowMultipart = true, long maxFileSize = 10_000)
    {
        var nodes = NodesWorkflowBuilder.Create()
                                        .AddNext(new HttpInNode { Method = "POST", UrlPattern = "/file", AllowMultipart = allowMultipart, MaxFileSize = maxFileSize.ToHumanizedSize() })
                                        .AddNext(new TestCallBackNode() { Callback = (input, parameters) => callback?.Invoke(input) })
                                        .AddNext(new HttpResponseNode())
                                        .BuildWithFlowNode();
        _nodeService.Deploy(nodes);
    }

    [IntegrationFact]
    public async Task AcceptForm_ValidFormReadFields_ShouldSuccess()
    {
        //Arrange
        _ = nameof(HttpInNodeImpl.Execute);
        var client = AppFixture.GetClient();

        SetupNodesForUpload(callback, allowMultipart: false);

        //Act
        var result = await client.Request(_apiUrl, "/file")
                                    .PostUrlEncodedAsync(
                                        new
                                        {
                                            user = "myUsername",
                                            pass = "myPassword"
                                        })
                                    .ReceiveJson<Dictionary<string, string>>();

        //Assert
        void callback(NodeMsg msg)
        {
            msg.Payload.Should().BeAssignableTo<IFormCollection>();
            var form = (IFormCollection)msg.Payload!;
            form["user"].Should().BeEquivalentTo("myUsername");
            form["pass"].Should().BeEquivalentTo("myPassword");
        }

        result.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["user"] = "myUsername",
            ["pass"] = "myPassword"
        });
    }

    [IntegrationFact]
    public async Task UploadFile_ValidFile_ShouldSuccess()
    {
        //Arrange
        _ = nameof(HttpInNodeImpl.Execute);
        var client = AppFixture.GetClient();

        var fileContent = "TEST-text";
        var fileName = "file1.txt";
        SetupNodesForUpload(callback);

        //Act
        var result = await client.Request(_apiUrl, "/file")
                                .PostMultipartAsync(mp => mp
                                    .AddString("str1", "str1_text")
                                    .AddFile("file", GenerateStreamFromString(fileContent), fileName)
                                )
                                .ReceiveString();

        //Assert
        void callback(NodeMsg msg)
        {
            msg.Payload.Should().BeAssignableTo<IFormCollection>();
            var form = (IFormCollection)msg.Payload!;
            form.Should().ContainKey("str1").WhoseValue.First().Should().Be("str1_text");
            form.Files.Should().ContainSingle();
            using var stream = form.Files.First().OpenReadStream();
            using var reader = new StreamReader(stream);
            string readedFileContent = reader.ReadToEndAsync().GetAwaiter().GetResult();
            readedFileContent.Should().Be(fileContent);
        }
    }

    [IntegrationFact]
    public async Task UploadFile_DisallowMultipartNode_ShouldUnsupportedStatusError()
    {
        //Arrange
        _ = nameof(HttpInNodeImpl.Execute);
        var client = AppFixture.GetClient();

        var fileContent = "TEST-text";
        var fileName = "file1.txt";
        SetupNodesForUpload(null, allowMultipart: false);

        //Act
        var result = await client.Request(_apiUrl, "/file")
                                .AllowAnyHttpStatus()
                                .PostMultipartAsync(mp => mp
                                    .AddFile("file", GenerateStreamFromString(fileContent), fileName)
                                );

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status415UnsupportedMediaType);
    }

    [IntegrationFact]
    public async Task UploadFile_SizeExceedsLimit_ThrowsPayloadTooLargeException()
    {
        //Arrange
        _ = nameof(HttpInNodeImpl.Execute);
        var client = AppFixture.GetClient();

        var fileName = "file1.txt";
        SetupNodesForUpload(null, maxFileSize: 1000);

        //Act
        var result = await client.Request(_apiUrl, "/file")
                                .AllowAnyHttpStatus()
                                .PostMultipartAsync(mp => mp
                                    .AddFile("file", GenerateStreamOfSize(2000), fileName)
                                );

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status413PayloadTooLarge);
    }

    private MemoryStream GenerateStreamFromString(string value)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
    }

    private MemoryStream GenerateStreamOfSize(ulong sizeInBytes)
    {
        var buffer = new byte[sizeInBytes];
        // Можно заполнить чем угодно, например случайными байтами:
        new Random().NextBytes(buffer);

        return new MemoryStream(buffer);
    }
}
