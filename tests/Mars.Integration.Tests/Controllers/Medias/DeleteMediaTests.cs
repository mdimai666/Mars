using System.Text;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Options.Models;
using Mars.Shared.Contracts.Files;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.Medias;

public class DeleteMediaTests : ApplicationTests
{
    private const string _apiUrl = "/api/Media";
    private readonly IOptionService _optionService;
    private readonly FileHostingInfo _fileHostingInfo;
    private readonly MediaOption _mediaOption;
    private readonly string _exampleFilesPath;
    private readonly string _image_CreationOfSpace1jpg;

    public DeleteMediaTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _optionService = AppFixture.ServiceProvider.GetRequiredService<IOptionService>();
        _fileHostingInfo = _optionService.FileHostingInfo();
        _mediaOption = _optionService.GetOption<MediaOption>();
        _mediaOption.IsAutoResizeUploadImage = true;
        _optionService.SetOptionOnMemory(_mediaOption);
        _exampleFilesPath = Path.Join(Directory.GetCurrentDirectory(), "..\\..\\..", "Controllers\\Medias\\ExampleFiles\\");
        _image_CreationOfSpace1jpg = "creation of space1.jpg";

    }

    private MemoryStream GenerateStreamFromString(string value)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
    }

    [IntegrationFact]
    public async Task Delete_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(MediaController.Delete);
        var client = AppFixture.GetClient();
        var fileContent = "TEST-text";
        var fileName = "file1.txt";

        var uploadFile = await client.Request(_apiUrl, "Upload")
                                .PostMultipartAsync(mp => mp
                                    .AddFile("file", GenerateStreamFromString(fileContent), fileName)
                                )
                                .CatchUserActionError()
                                .ReceiveJson<FileDetailResponse>();

        //Act
        var result = await client.Request(_apiUrl, uploadFile.Id).DeleteAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        var ef = AppFixture.MarsDbContext();
        var dbFile = ef.Files.FirstOrDefault(s => s.Id == uploadFile.Id);
        dbFile.Should().BeNull();
        var fullPath = _fileHostingInfo.FileAbsolutePath(uploadFile.FilePhysicalPath);
        File.Exists(fullPath).Should().BeFalse();
    }

    [IntegrationFact]
    public async Task DeleteMany_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(MediaController.DeleteMany);
        var client = AppFixture.GetClient();
        var fileContent = "TEST-text";
        var fileName = "file1.txt";

        var uploadFile = await client.Request(_apiUrl, "Upload")
                                .PostMultipartAsync(mp => mp
                                    .AddFile("file", GenerateStreamFromString(fileContent), fileName)
                                )
                                .CatchUserActionError()
                                .ReceiveJson<FileDetailResponse>();

        //Act
        var result = await client.Request(_apiUrl, "DeleteMany").AppendQueryParam(new { ids = new Guid[] { uploadFile.Id } }).DeleteAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        var ef = AppFixture.MarsDbContext();
        var dbFile = ef.Files.FirstOrDefault(s => s.Id == uploadFile.Id);
        dbFile.Should().BeNull();
        var fullPath = _fileHostingInfo.FileAbsolutePath(uploadFile.FilePhysicalPath);
        File.Exists(fullPath).Should().BeFalse();
    }

}
