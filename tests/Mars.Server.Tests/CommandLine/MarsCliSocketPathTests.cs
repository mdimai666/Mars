using System.Text.RegularExpressions;
using Mars.CommandLine.Remote;

namespace Test.Mars.Server.CommandLine;

public partial class MarsCliSocketPathTests
{
    [GeneratedRegex(@"^mars-[A-Za-z0-9._-]{1,24}-[0-9a-f]{16}\.sock$")]
    private static partial Regex SocketFileNameRegex();

    [Fact]
    public void GetSocketPath_SameInput_GivesSamePath()
    {
        var cwd = Path.Combine(Path.GetTempPath(), "mars-cli-tests");
        var a = MarsCliSocket.GetSocketPath(cwd, ["node", "list"]);
        var b = MarsCliSocket.GetSocketPath(cwd, ["ds", "backup"]);

        Assert.Equal(a, b); // аргументы команды на фингерпринт не влияют
    }

    [Fact]
    public void GetSocketPath_DifferentDirectories_GiveDifferentPaths()
    {
        var root = Path.GetTempPath();
        var a = MarsCliSocket.GetSocketPath(Path.Combine(root, "mars-a"), []);
        var b = MarsCliSocket.GetSocketPath(Path.Combine(root, "mars-b"), []);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void GetSocketPath_DifferentCfg_GiveDifferentPaths()
    {
        var cwd = Path.Combine(Path.GetTempPath(), "mars-cli-tests");
        var a = MarsCliSocket.GetSocketPath(cwd, ["-cfg", "one.json", "node", "list"]);
        var b = MarsCliSocket.GetSocketPath(cwd, ["-cfg", "two.json", "node", "list"]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void GetSocketPath_RelativeCfg_EquivalentToAbsolute()
    {
        var cwd = Path.Combine(Path.GetTempPath(), "mars-cli-tests");
        var relative = MarsCliSocket.GetSocketPath(cwd, ["-cfg", "cfg.json"]);
        var absolute = MarsCliSocket.GetSocketPath(cwd, ["--config", Path.Combine(Directory.GetCurrentDirectory(), "cfg.json")]);

        Assert.Equal(relative, absolute);
    }

    [Fact]
    public void GetSocketPath_ShortNameUnderTemp()
    {
        var path = MarsCliSocket.GetSocketPath(Path.Combine(Path.GetTempPath(), "mars-cli-tests"), []);

        Assert.StartsWith(Path.GetTempPath(), path);
        Assert.True(path.Length < 108, $"socket path too long for sun_path: {path}");
        Assert.Matches(SocketFileNameRegex(), Path.GetFileName(path));
    }

    [Fact]
    public void GetSocketPath_WeirdDirectoryName_SanitizesLabel()
    {
        var weird = Path.Combine(Path.GetTempPath(), "dir with spaces!!", "My App #$%");
        var path = MarsCliSocket.GetSocketPath(weird, []);

        Assert.Matches(SocketFileNameRegex(), Path.GetFileName(path));
    }

    [Fact]
    public async Task ProbeAsync_NoFile_ReturnsNull()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mars-test-{Guid.NewGuid():N}.sock");
        Assert.Null(await MarsCliSocket.ProbeAsync(path));
    }

    [Fact]
    public async Task ProbeAsync_StaleRegularFile_ReturnsNull()
    {
        // файл остался от упавшего процесса: connect падает с ConnectionRefused
        var path = Path.Combine(Path.GetTempPath(), $"mars-test-{Guid.NewGuid():N}.sock");
        await File.WriteAllTextAsync(path, "stale");
        try
        {
            Assert.Null(await MarsCliSocket.ProbeAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
