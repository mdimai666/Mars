using FluentAssertions;
using Mars.Nodes.Core.Implements.Utils;

namespace Mars.Nodes.Tests.Utils;

public class FileListUtilityTests : IDisposable
{
    private readonly DirectoryInfo _root;
    private readonly string _soundsDir;

    public FileListUtilityTests()
    {
        _root = Directory.CreateTempSubdirectory("mars-filelist-");

        // внешняя папка с .gitignore, режущим папку data (сценарий: хост-репо
        // с /data в .gitignore, внутри которого лежит читаемая папка)
        File.WriteAllText(Path.Combine(_root.FullName, ".gitignore"), "/data\n");

        _soundsDir = Path.Combine(_root.FullName, "data", "sounds");
        Directory.CreateDirectory(Path.Combine(_soundsDir, "sub"));
        File.WriteAllText(Path.Combine(_soundsDir, "a.mp3"), "");
        File.WriteAllText(Path.Combine(_soundsDir, "b.wav"), "");
        File.WriteAllText(Path.Combine(_soundsDir, "sub", "c.mp3"), "");
    }

    public void Dispose()
    {
        try { _root.Delete(recursive: true); }
        catch { /* временная папка */ }
    }

    [Fact]
    public void GetFiles_WithoutRootGitIgnore_AncestorGitIgnoredNotApplied()
    {
        //Act
        var files = new FileListUtility().GetFiles(_soundsDir,
                                                    includeFilter: ".mp3,.wav",
                                                    maxDepth: 3,
                                                    returnRelativePaths: true,
                                                    useRootGitIgnore: false);

        //Assert
        files.Select(f => f.Replace('\\', '/'))
             .Should().BeEquivalentTo("a.mp3", "b.wav", "sub/c.mp3");
    }

    [Fact]
    public void GetFiles_WithoutRootGitIgnore_InnerGitIgnoreStillApplied()
    {
        //Arrange
        File.WriteAllText(Path.Combine(_soundsDir, ".gitignore"), "*.wav\n");

        //Act
        var files = new FileListUtility().GetFiles(_soundsDir,
                                                    includeFilter: ".mp3,.wav",
                                                    maxDepth: 3,
                                                    returnRelativePaths: true,
                                                    useRootGitIgnore: false);

        //Assert
        files.Select(f => f.Replace('\\', '/'))
             .Should().BeEquivalentTo("a.mp3", "sub/c.mp3");
    }

    [Fact]
    public void GetFiles_WithRootGitIgnore_RepositoryGitIgnoreApplied()
    {
        //Arrange
        Directory.CreateDirectory(Path.Combine(_root.FullName, ".git"));

        //Act
        var files = new FileListUtility().GetFiles(_soundsDir,
                                                    includeFilter: ".mp3,.wav",
                                                    maxDepth: 3,
                                                    returnRelativePaths: true,
                                                    useRootGitIgnore: true);

        //Assert
        files.Should().BeEmpty();
    }
}
