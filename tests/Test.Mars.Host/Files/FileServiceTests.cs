using System.Text;
using Mars.Host.Services;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Options.Models;
using Mars.Test.Common.Constants;
using FluentAssertions;
using NSubstitute;

namespace Test.Mars.Host.Files;

public class FileServiceTests
{

    private readonly InMemoryFileStorage _inMemoryFileStorage;
    private readonly IOptionService _optionService;
    private readonly IFileRepository _fileRepository;
    private readonly IImageProcessor _imageProcessor;
    private readonly FileHostingInfo _fileHostingInfo;
    private readonly FileService _fileService;

    public FileServiceTests()
    {
        _inMemoryFileStorage = new InMemoryFileStorage();
        _optionService = Substitute.For<IOptionService>();
        _fileRepository = Substitute.For<IFileRepository>();
        _imageProcessor = Substitute.For<IImageProcessor>();

        _fileHostingInfo = new FileHostingInfo
        {
            Backend = new Uri("http://localhost"),
            UploadSubPath = "upload",
            wwwRoot = new Uri("C:\\www\\mars\\wwwRoot"),
        };

        _optionService.FileHostingInfo().Returns(_fileHostingInfo);

        _fileService = new FileService(_inMemoryFileStorage, _optionService, _fileRepository, _imageProcessor);
    }

    [Fact]
    public async Task WriteFile_SuccessWritedAndRead_ShouldRead()
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;
        var newFileName = "file1.txt";
        var newFileContent = "OK text";
        var userId = UserConstants.TestUserId;
        var returnFileId = Guid.NewGuid();

        _fileRepository
            .Create(Arg.Any<CreateFileQuery>(), _fileHostingInfo, default)
            .Returns(returnFileId);

        var createdFileId = await _fileService.WriteUpload(newFileName, "path1", Encoding.UTF8.GetBytes(newFileContent), userId, cancellationToken);

        // Act
        //var content = await _fileService.ReadFile($"path1/{newFileName}", cancellationToken);
        var content = _inMemoryFileStorage.ReadAllText($"path1/{newFileName}");

        // Assert
        content.Should().Be(newFileContent);
        createdFileId.Should().Be(returnFileId);
        _ = _fileRepository.Received(1).Create(Arg.Any<CreateFileQuery>(), _fileHostingInfo, cancellationToken);
    }

    [Fact]
    public void GetFileMeta_GenerateValidData_Success()
    {
        // Arrange
        var fileName = "file1.png";
        var fileDir = "Media/sub2/";
        var filePath = fileDir + fileName;
        var filenameWtEx = Path.GetFileNameWithoutExtension(fileName);

        var mediaOption = new MediaOption()
        {
            ImagePreviewSizeConfigs = MediaOption.DefaultImagePreviewSizeConfigs
        };

        // Act
        var meta = _fileService.GenerateThumbnailsAndGetFileMeta(filePath, "png", mediaOption, 200, 100);

        // Assert
        meta.Thumbnails.Count.Should().Be(mediaOption.ImagePreviewSizeConfigs.Length);
        foreach (var d in meta.Thumbnails)
        {
            var size = d.Key;
            var t = d.Value;
            t.FilePath.Should().StartWith($"{FileService.MediaThumbsDirName}/Media/sub2");
            t.FilePath.Should().EndWith($"_{size}.webp");
            var thumbFileName = $"{filenameWtEx}_{size}.webp";
            t.FilePath.Should().Be($"{FileService.MediaThumbsDirName}/{fileDir}{thumbFileName}");

            t.FileUrl.Should().Be($"/upload/{FileService.MediaThumbsDirName}/{fileDir}{thumbFileName}");
        }
    }
}
