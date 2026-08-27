using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Test.Mars.Host.Files;

public class FileStorageMoveTests : IDisposable
{
    private readonly string _rootPath;
    private readonly FileStorage _fileStorage;

    public FileStorageMoveTests()
    {
        _rootPath = Path.Join(Path.GetTempPath(), "mars-tests", "file-storage-move", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootPath);

        var fileHostingInfo = new FileHostingInfo
        {
            Backend = new Uri("http://localhost"),
            RequestPath = "upload",
            PhysicalPath = new Uri(_rootPath),
        };

        _fileStorage = new FileStorage(Options.Create(fileHostingInfo));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, true);
        }
    }

    [Fact]
    public void MoveFile_ReadNewPath_ShouldSuccess()
    {
        // Arrange
        _fileStorage.CreateDirectory("Media");
        _fileStorage.Write("Media/file1.txt", "content1");
        _fileStorage.CreateDirectory("Media/2026");

        // Act
        _fileStorage.MoveFile("Media/file1.txt", "Media/2026/file1.txt");

        // Assert
        _fileStorage.ReadAllText("Media/2026/file1.txt").Should().Be("content1");
        _fileStorage.FileExists("Media/file1.txt").Should().BeFalse();
    }

    [Fact]
    public void MoveDirectory_NestedContent_ShouldMoveAll()
    {
        // Arrange
        _fileStorage.CreateDirectory("Media/2026");
        _fileStorage.Write("Media/2026/file1.txt", "content1");
        _fileStorage.CreateDirectory("Media/2026/sub");
        _fileStorage.Write("Media/2026/sub/file2.txt", "content2");

        // Act
        _fileStorage.MoveDirectory("Media/2026", "Media/Archive");

        // Assert
        _fileStorage.DirectoryExists("Media/Archive").Should().BeTrue();
        _fileStorage.DirectoryExists("Media/Archive/sub").Should().BeTrue();
        _fileStorage.DirectoryExists("Media/2026").Should().BeFalse();
        _fileStorage.ReadAllText("Media/Archive/file1.txt").Should().Be("content1");
        _fileStorage.ReadAllText("Media/Archive/sub/file2.txt").Should().Be("content2");
    }
}
