using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Test.Mars.Host.Files;

public class ReadFileStorageTests
{
    private readonly FileHostingInfo _fileHostingInfo;
    private readonly FileStorage _fileStorage;
    private readonly string _exampleFilesPath;

    public ReadFileStorageTests()
    {
        // C:\Users\D\Documents\VisualStudio\2025\Mars\Tests\Test.Mars.Host\bin\Debug\net8.0\Files\ExampleFiles
        //var filesPath = Path.Join(Directory.GetCurrentDirectory(), "Files", "ExampleFiles");
        _exampleFilesPath = Path.Join(Directory.GetCurrentDirectory(), "Files", "ExampleFiles");
        var filesPath = Path.Join(Directory.GetCurrentDirectory(), "Files");

        _fileHostingInfo = new FileHostingInfo
        {
            Backend = new Uri("http://localhost"),
            UploadSubPath = "ExampleFiles",
            wwwRoot = new Uri(filesPath),
        };

        _fileStorage = new FileStorage(Options.Create(_fileHostingInfo));
    }

    [Fact]
    public void GetDirectoryContents_ListFiles_ListSuccess()
    {
        // Arrange
        var expectFileCounts = 3;

        // Act
        var files = _fileStorage.GetDirectoryContents("").ToList();

        // Assert
        files.Count().Should().Be(expectFileCounts);
        files[0].IsDirectory.Should().BeTrue();
        files[0].Name.Should().Be("SubPath");
        files[1].Name.Should().Be("file1.txt");
        files[2].Name.Should().Be("file2.txt");
    }

    [Fact]
    public void Read_FileContent_ShouldRead()
    {
        // Arrange
        var filename = "file1.txt";
        var expectContent = File.ReadAllText(Path.Join(_exampleFilesPath, filename));

        // Act
        var fileContent = _fileStorage.ReadAllText(filename);

        // Assert
        fileContent.Should().Be(expectContent);
    }

    [Fact]
    public void Read_NotExistFile_ShouldException()
    {
        // Arrange
        var filename = "invalid_file_name.ttt";

        // Act
        var action = () => _fileStorage.ReadAllText(filename);

        // Assert
        action.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void Read_FileContentFromBytes_ShouldRead()
    {
        // Arrange
        var filename = "file1.txt";
        var expectContent = File.ReadAllBytes(Path.Join(_exampleFilesPath, filename));

        // Act
        var fileContent = _fileStorage.Read(filename);

        // Assert
        fileContent.Should().BeEquivalentTo(expectContent);
    }

    [Fact]
    public void Read_FileContentFromStream_ShouldRead()
    {
        // Arrange
        var filename = "file1.txt";
        var expectContent = File.ReadAllText(Path.Join(_exampleFilesPath, filename));

        // Act
        _fileStorage.Read(filename, out var stream);
        using var streamReader = new StreamReader(stream);
        var fileContent = streamReader.ReadToEnd();

        // Assert
        fileContent.Should().Be(expectContent);
    }
}
