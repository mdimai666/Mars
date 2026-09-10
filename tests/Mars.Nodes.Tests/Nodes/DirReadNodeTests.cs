using FluentAssertions;
using Mars.Nodes.Core.Implements.Nodes.Storage;
using Mars.Nodes.Tests.Services;
using Mars.Test.Common.Helpers;

namespace Mars.Nodes.Tests.Nodes;

public class DirReadNodeTests : NodeServiceUnitTestBase
{
    string _testFilesDir;

    public DirReadNodeTests()
    {
        _testFilesDir = SolutionPathHelper.Resolve("tests", "Mars.Nodes.Tests", "TestFiles");
    }

    private string FP(string path) => Path.GetFullPath(Path.Combine(_testFilesDir, path));
    private string[] FP(string[] paths) => [.. paths.Select(FP)];

    [Fact]
    public async Task ReadFiles_ListRootFiles_Succeeds()
    {
        //Arrange
        _ = nameof(DirReadNodeImpl.Execute);
        var node = new DirReadNode { DirPath = _testFilesDir, ReturnRelativePath = false };
        var expectFiles = FP(["JsonFile.json", "TextFile1.txt"]);

        //Act
        var msg = await ExecuteNode(node);

        //Assert
        var files = (string[])msg.Payload!;
        files.Should().BeEquivalentTo(expectFiles);
    }

    [Fact]
    public async Task ReadFiles_ListRecurseFiles_Succeeds()
    {
        //Arrange
        _ = nameof(DirReadNodeImpl.Execute);
        var node = new DirReadNode { DirPath = _testFilesDir, ReturnRelativePath = false, MaxDepth = 0 };
        var expectFiles = FP(["JsonFile.json", "TextFile1.txt", "SubDir\\TextFileSub1.txt", "SubDir\\GrandSonDir\\GrandSonTextFile.txt"]);

        //Act
        var msg = await ExecuteNode(node);

        //Assert
        var files = (string[])msg.Payload!;
        files.Should().BeEquivalentTo(expectFiles);
    }

    [Fact]
    public async Task ReadFiles_Depth2_Succeeds()
    {
        //Arrange
        _ = nameof(DirReadNodeImpl.Execute);
        var node = new DirReadNode { DirPath = _testFilesDir, ReturnRelativePath = false, MaxDepth = 2 };
        var expectFiles = FP(["JsonFile.json", "TextFile1.txt", "SubDir\\TextFileSub1.txt"]);

        //Act
        var msg = await ExecuteNode(node);

        //Assert
        var files = (string[])msg.Payload!;
        files.Should().BeEquivalentTo(expectFiles);
    }

    [Fact]
    public async Task ReadFiles_ListByPattern_Succeeds()
    {
        //Arrange
        _ = nameof(DirReadNodeImpl.Execute);
        var node = new DirReadNode { DirPath = _testFilesDir, ReturnRelativePath = false, MaxDepth = 0, Pattern = ".txt" };
        var expectFiles = FP(["TextFile1.txt", "SubDir\\TextFileSub1.txt", "SubDir\\GrandSonDir\\GrandSonTextFile.txt"]);

        //Act
        var msg = await ExecuteNode(node);

        //Assert
        var files = (string[])msg.Payload!;
        files.Should().BeEquivalentTo(expectFiles);
    }
}
