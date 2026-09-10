using FluentAssertions;
using Mars.Server.Abstractions.Services;
using Mars.Storage.Services;

namespace Mars.Server.Tests.Files;

public class InMemoryFileStorageTests
{
    InMemoryFileStorage GetStorage() => new(new Dictionary<string, string>()
    {
        ["files/text.txt"] = "OK",
        ["files/second file.txt"] = "second",
        ["root-file.bin"] = "123",
    });

    const string TextFilepath1 = "files/text.txt";


    [Fact]
    public void ReadFile_ExistFile_Succeeds()
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
        storage.DeleteFile(TextFilepath1);
        var action = () => storage.ReadAllText(TextFilepath1);

        // Assert
        action.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void CreateFile_ReadContent_Succeeds()
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
    public void ReadFile_PassAnySlashed_Succeeds(string filepath)
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
    public void FileInfo_FileLength_IsGreaterThanZero(string filepath)
    {
        // Arrange
        var storage = GetStorage();

        // Act
        var fileInfo = storage.GetFileInfo(filepath);

        // Assert
        fileInfo.Should().NotBeNull();
        fileInfo!.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void MoveFile_ReadNewPath_Succeeds()
    {
        // Arrange
        var storage = GetStorage();
        const string newPath = "archive/text.txt";

        // Act
        storage.MoveFile(TextFilepath1, newPath);

        // Assert
        storage.ReadAllText(newPath).Should().Be("OK");
        var action = () => storage.ReadAllText(TextFilepath1);
        action.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void MoveFile_NotExistFile_Throws()
    {
        // Arrange
        var storage = GetStorage();

        // Act
        var action = () => storage.MoveFile("not-exist.txt", "archive/not-exist.txt");

        // Assert
        action.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void MoveDirectory_ReadMovedContent_Succeeds()
    {
        // Arrange
        var storage = GetStorage();
        storage.CreateDirectory("files");
        storage.CreateDirectory("files/sub");
        storage.Write("files/sub/inner.txt", "inner");

        // Act
        storage.MoveDirectory("files", "archive");

        // Assert
        storage.DirectoryExists("archive").Should().BeTrue();
        storage.DirectoryExists("archive/sub").Should().BeTrue();
        storage.DirectoryExists("files").Should().BeFalse();
        storage.ReadAllText("archive/text.txt").Should().Be("OK");
        storage.ReadAllText("archive/second file.txt").Should().Be("second");
        storage.ReadAllText("archive/sub/inner.txt").Should().Be("inner");
        storage.ReadAllText("root-file.bin").Should().Be("123");
    }

    [Fact]
    public void GetDirectoryContents_NestedFiles_ReturnsOnlyDirectChildren()
    {
        // Arrange
        var storage = new InMemoryFileStorage();
        storage.Write("Media/2026/one.txt", "one");
        storage.Write("Media/2026/deep/two.txt", "two");
        storage.Write("MediaThumbs/2026/thumb.webp", "thumb");

        // Act
        var names = storage.GetDirectoryContents("Media").Select(s => s.Name).ToList();

        // Assert
        names.Should().BeEquivalentTo(["2026"]);
        storage.GetDirectoryContents("Media").Single().IsDirectory.Should().BeTrue();
        storage.GetDirectoryContents("Media/2026").Select(s => s.Name).Should().BeEquivalentTo(["deep", "one.txt"]);
    }

    [Fact]
    public void GetDirectoryContents_SimilarPrefixes_DoesNotMixRoots()
    {
        // Arrange
        var storage = new InMemoryFileStorage();
        storage.CreateDirectory("Media");
        storage.CreateDirectory("MediaThumbs");

        // Act & Assert
        storage.GetDirectoryContents("Media").Should().BeEmpty();
        storage.GetDirectoryContents("").Select(s => s.Name).Should().BeEquivalentTo(["Media", "MediaThumbs"]);
    }

    [Fact]
    public void DeleteDirectory_SimilarPrefix_DeletesOnlyOwnTree()
    {
        // Arrange
        var storage = new InMemoryFileStorage();
        storage.Write("Media/2026/one.txt", "one");
        storage.Write("MediaThumbs/2026/thumb.webp", "thumb");

        // Act
        storage.DeleteDirectory("Media", true);

        // Assert
        storage.DirectoryExists("Media").Should().BeFalse();
        storage.FileExists("Media/2026/one.txt").Should().BeFalse();
        storage.FileExists("MediaThumbs/2026/thumb.webp").Should().BeTrue();
        storage.DirectoryExists("MediaThumbs/2026").Should().BeTrue();
    }

    [Fact]
    public void DeleteDirectory_NotEmptyAndNotRecursive_Throws()
    {
        // Arrange
        var storage = new InMemoryFileStorage();
        storage.Write("Media/one.txt", "one");

        // Act
        var action = () => storage.DeleteDirectory("Media", false);

        // Assert
        action.Should().Throw<IOException>();
        storage.FileExists("Media/one.txt").Should().BeTrue();
    }

    [Fact]
    public void DeleteDirectory_EmptyAndNotRecursive_Succeeds()
    {
        // Arrange
        var storage = new InMemoryFileStorage();
        storage.CreateDirectory("Media/2026");

        // Act
        storage.DeleteDirectory("Media/2026", false);

        // Assert
        storage.DirectoryExists("Media/2026").Should().BeFalse();
        storage.DirectoryExists("Media").Should().BeTrue();
    }

    [Fact]
    public void CreateDirectory_NestedPath_CreatesParentSegments()
    {
        // Arrange
        var storage = new InMemoryFileStorage();

        // Act
        storage.CreateDirectory("Media/2026/sub");

        // Assert
        storage.DirectoryExists("Media").Should().BeTrue();
        storage.DirectoryExists("Media/2026").Should().BeTrue();
        storage.DirectoryExists("Media/2026/sub").Should().BeTrue();
    }

    [Fact]
    public void Write_NestedPath_CreatesParentDirectory()
    {
        // Arrange
        var storage = new InMemoryFileStorage();

        // Act
        storage.Write("Media/2026/one.txt", "one");

        // Assert
        storage.DirectoryExists("Media").Should().BeTrue();
        storage.DirectoryExists("Media/2026").Should().BeTrue();
    }

    [Fact]
    public void OpenRead_ReturnedStream_IsReadOnly()
    {
        // Arrange
        var storage = GetStorage();

        // Act
        using var stream = storage.OpenRead(TextFilepath1);
        var action = () => stream.Write([1]);

        // Assert
        stream.CanWrite.Should().BeFalse();
        action.Should().Throw<NotSupportedException>();
        storage.ReadAllBytes(TextFilepath1).Should().BeEquivalentTo("OK"u8.ToArray());
    }

    [Fact]
    public void GetFileInfo_NotExistFile_ReturnsNull()
    {
        // Arrange
        var storage = GetStorage();

        // Act & Assert
        storage.GetFileInfo("not-exist.txt").Should().BeNull();
        storage.GetFileInfo(TextFilepath1).Should().NotBeNull();
    }
}
