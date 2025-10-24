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

/// <seealso cref="Mars.Controllers.MediaController"/>
public sealed class UploadMediaTests : ApplicationTests
{
    private const string _apiUrl = "/api/Media/Upload";
    private readonly IOptionService _optionService;
    private readonly FileHostingInfo _fileHostingInfo;
    private readonly MediaOption _mediaOption;
    private readonly string _exampleFilesPath;
    private readonly string _image_CreationOfSpace1jpg;

    public UploadMediaTests(ApplicationFixture appFixture) : base(appFixture)
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

    [IntegrationFact]
    public async Task Upload_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(MediaController.Upload);
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request(_apiUrl).AllowAnyHttpStatus().PostMultipartAsync(mp => mp.AddString("s", "v"));

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task Upload_TextFileUploadRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(MediaController.Upload);
        var client = AppFixture.GetClient();
        var fileContent = "TEST-text";
        var fileName = "file1.txt";

        //Act
        var result = await client.Request(_apiUrl)
                                .PostMultipartAsync(mp => mp
                                    //.AddString("file_group", "some group")
                                    .AddFile("file", GenerateStreamFromString(fileContent), fileName)
                                )
                                .CatchUserActionError()
                                .ReceiveJson<FileDetailResponse>();

        //Assert
        result.Should().NotBeNull();
        using var ef = AppFixture.MarsDbContext();
        var dbFile = ef.Files.FirstOrDefault(s => s.Id == result.Id);
        dbFile.Should().NotBeNull();
        dbFile.FileSize.Should().Be((ulong)fileContent.Length);
        dbFile.FileName.Should().Be(fileName);
        dbFile.FileExt.Should().Be("txt");

        var fullPath = _fileHostingInfo.FileAbsolutePath(dbFile.FilePhysicalPath);
        File.Exists(fullPath).Should().BeTrue();
        var writtedFileContent = File.ReadAllText(fullPath);
        writtedFileContent.Should().Be(fileContent);

    }

    [IntegrationFact]
    public async Task Upload_ImageUploadMustCreateThumbnails_ShouldSuccess()
    {
        //Arrange
        _ = nameof(MediaController.Upload);
        var client = AppFixture.GetClient();
        var image1FilePath = Path.Join(_exampleFilesPath, _image_CreationOfSpace1jpg);
        var fs = AppFixture.ServiceProvider.GetRequiredService<IFileStorage>();

        //Act
        var result = await client.Request(_apiUrl)
                                .PostMultipartAsync(mp => mp
                                    //.AddString("file_group", "some group")
                                    .AddFile("file", image1FilePath, _image_CreationOfSpace1jpg)
                                )
                                .CatchUserActionError()
                                .ReceiveJson<FileDetailResponse>();

        //Assert
        // 1. записано в БД
        result.Should().NotBeNull();
        using var ef = AppFixture.MarsDbContext();
        var dbFile = ef.Files.FirstOrDefault(s => s.Id == result.Id);
        dbFile.Should().NotBeNull();
        dbFile.FileSize.Should().BeGreaterThan(0);
        dbFile.FileName.Should().Be(_image_CreationOfSpace1jpg);
        dbFile.FileExt.Should().Be("jpg");

        // 2. записан файл auto resized
        var fullPath = _fileHostingInfo.FileAbsolutePath(dbFile.FilePhysicalPath);
        File.Exists(fullPath).Should().BeTrue();
        var writtedFileLength = (ulong)(new FileInfo(fullPath).Length);
        writtedFileLength.Should().Be(dbFile.FileSize);

        // 3. созданы миниатюрные эскизы
        dbFile.Meta.Should().NotBeNull();
        dbFile.Meta.ImageInfo.Width.Should().NotBe(0);
        dbFile.Meta.ImageInfo.Height.Should().NotBe(0);
        dbFile.Meta.Thumbnails!.Count().Should().Be(_mediaOption.ImagePreviewSizeConfigs.Length);
        foreach (var d in dbFile.Meta.Thumbnails)
        {
            var size = d.Key;
            var mini = d.Value;
            var cfg = _mediaOption.ImagePreviewSizeConfigs.First(s => s.Name == size);
            var fullpath = _fileHostingInfo.FileAbsolutePath(mini.FilePath);

            File.Exists(fullpath).Should().BeTrue();
            fs.FileExists(mini.FilePath).Should().BeTrue();
            mini.Width.Should().BeLessThanOrEqualTo(cfg.Width);
            mini.Height.Should().BeLessThanOrEqualTo(cfg.Height);
        }
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

    void SetupMediaOption(Action<MediaOption> action)
    {
        action(_mediaOption);
        _optionService.SetOptionOnMemory(_mediaOption);
    }

    void SetupMediaMaxInpuFileSize(ulong maxInpuFileSize = 10 * 1024 * 1024)
        => SetupMediaOption(op => op.MaximumInputFileSize = maxInpuFileSize);

    ulong _fileTooLargeSize = 15 * 1024 * 1024;

    [IntegrationFact]
    public async Task Upload_FileTooLarge_ShouldValidateError()
    {
        //Arrange
        _ = nameof(MediaController.Upload);
        _ = nameof(UploadMediaFileValidator);
        var client = AppFixture.GetClient();
        var fileName = "file1.txt";
        SetupMediaMaxInpuFileSize();

        //Act
        var result = await client.Request(_apiUrl)
                                .PostMultipartAsync(mp => mp
                                    //.AddString("file_group", "some group")
                                    .AddFile("file", GenerateStreamOfSize(_fileTooLargeSize), fileName)
                                ).ReceiveValidationError();

        //Assert
        result.Should().NotBeNull();
        result.Errors.Should().HaveCount(1)
                .And.Subject.Values.First().Should().ContainMatch("*max file size*");

    }

    [IntegrationFact]
    public async Task Upload_FileTooLargeCheckAspNetConfiguration_ShouldSuccess()
    {
        //Arrange
        _ = nameof(MediaController.Upload);
        _ = nameof(UploadMediaFileValidator);
        var client = AppFixture.GetClient();
        var fileName = "file1.txt";
        SetupMediaMaxInpuFileSize(205 * 1024 * 1024);

        //Act
        var result = await client.Request(_apiUrl)
                                .PostMultipartAsync(mp => mp
                                    //.AddString("file_group", "some group")
                                    .AddFile("file", GenerateStreamOfSize(205 * 1024 * 1024), fileName)
                                );

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [IntegrationFact]
    public async Task Upload_FileNotAllowedExtensions_ShouldValidateError()
    {
        //Arrange
        _ = nameof(MediaController.Upload);
        _ = nameof(UploadMediaFileValidator);
        var client = AppFixture.GetClient();
        var fileName = "file1.not_allow_ext";
        SetupMediaMaxInpuFileSize();

        //Act
        var result = await client.Request(_apiUrl)
                                .PostMultipartAsync(mp => mp
                                    //.AddString("file_group", "some group")
                                    .AddFile("file", GenerateStreamFromString("000"), fileName)
                                ).ReceiveValidationError();

        //Assert
        result.Should().NotBeNull();
        result.Errors.Should().HaveCount(1)
                .And.Subject.Values.First().Should().ContainMatch("*file extension is not allowed*");

    }
}
