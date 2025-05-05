using Mars.Host.Shared.Services;
using FluentAssertions;

namespace Test.Mars.Host.Files;

public class InMemoryFileStorageTests
{
    InMemoryFileStorage GetStorage() => new InMemoryFileStorage(new Dictionary<string, string>()
    {
        ["files/text.txt"] = "OK",
        ["files/second file.txt"] = "second",
        ["root-file.bin"] = "123",
    });

    const string TextFilepath1 = "files/text.txt";


    [Fact]
    public void ReadFile_ExistFile_ShouldSuccess()
    {
        // Arrange
        var storage = GetStorage();

        // Act
        var content = storage.ReadAllText(TextFilepath1);

        // Assert
        content.Should().Be("OK");
    }

    [Fact]
    public void ReadFile_NotValidPathButValidName_FailException()
    {
        // Arrange
        var storage = GetStorage();

        // Act
        var action = () => storage.ReadAllText("text.txt");

        // Assert
        action.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void DeleteFile_ReadDeleted_FailException()
    {
        // Arrange
        var storage = GetStorage();

        // Act
        storage.Delete(TextFilepath1);
        var action = () => storage.ReadAllText(TextFilepath1);

        // Assert
        action.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void CreateFile_ReadContent_ShouldSuccess()
    {
        // Arrange
        var storage = GetStorage();
        string newFileName = "file1.bin";
        string newFileContent = "success text";

        // Act
        storage.Write(newFileName, newFileContent);

        // Assert
        var content = storage.ReadAllText(newFileName);
        content.Should().Be(newFileContent);
    }

    [Fact]
    public void EnumerateFiles_Enumarate_ReturnValidNames()
    {
        // Arrange
        var storage = GetStorage();
        string[] fileNames = ["text.txt", "second file.txt"];

        // Act
        var directoryContent = storage.GetDirectoryContents("files");

        // Assert
        var directoryContentNames = directoryContent.Select(s => s.Name);
        directoryContentNames.Should().BeEquivalentTo(fileNames);
    }

    [Theory]
    [InlineData("files/text.txt")]
    [InlineData("files\\text.txt")]
    public void ReadFile_PassAnySlashed_ShouldSuccess(string filepath)
    {
        // Arrange
        var storage = GetStorage();

        // Act
        var content = storage.ReadAllText(filepath);

        // Assert
        content.Should().Be("OK");
    }

    [Theory]
    [InlineData("files/text.txt")]
    [InlineData("files\\text.txt")]
    public void FileInfo_FileLength_ShouldGreaterThanZero(string filepath)
    {
        // Arrange
        var storage = GetStorage();

        // Act
        var fileInfo = storage.FileInfo(filepath);

        // Assert
        fileInfo.Length.Should().BeGreaterThan(0);
    }
}
