using FluentAssertions;
using Mars.Server.Services;

namespace Mars.Integration.Tests.Services;

public class LogFileFilterTests : IDisposable
{
    static readonly TimeSpan Tz = TimeSpan.FromHours(9);

    readonly string dir = Path.Combine(Path.GetTempPath(), "mars-log-filter-tests", Guid.NewGuid().ToString("N"));

    // реальный формат NReco: табы, ISO-таймстемп с таймзоной
    static string Entry(DateTimeOffset ts, string level, string message = "message")
        => $"{ts:yyyy-MM-ddTHH:mm:ss.fffffffzzz}\t{level}\t[Test.Logger]\t[EventId]\t{message}";

    static string Entry(string ts, string level, string message = "message")
        => $"{ts}\t{level}\t[Test.Logger]\t[EventId]\t{message}";

    void WriteLogFile(string day, params string[] lines)
    {
        Directory.CreateDirectory(dir);
        File.WriteAllLines(Path.Combine(dir, $"app_{day}.log"), lines);
    }

    [Fact]
    public void FilterLines_RealFormat_ByLevels_KeepsWarnAndAbove()
    {
        var log = string.Join('\n',
            Entry("2026-08-11T11:00:00.0000000+09:00", "INFO"),
            Entry("2026-08-11T11:00:01.0000000+09:00", "WARN"),
            Entry("2026-08-11T11:00:02.0000000+09:00", "ERROR"),
            Entry("2026-08-11T11:00:03.0000000+09:00", "CRITICAL"));

        using var reader = new StringReader(log);
        var result = LogFileFilter.FilterLines(reader, ["WARN", "ERROR", "CRITICAL"], null).ToArray();

        result.Should().HaveCount(3);
        result.Should().NotContain(l => l.Contains("INFO"));
    }

    [Fact]
    public void FilterLines_KeepsMultilineEntryTogether()
    {
        var log = string.Join('\n',
            Entry("2026-08-11T11:00:00.0000000+09:00", "INFO"),
            Entry("2026-08-11T11:00:01.0000000+09:00", "ERROR", "boom"),
            "System.Exception: boom",
            "   at Mars.Something() in /src/Foo.cs:line 10");

        using var reader = new StringReader(log);
        var result = LogFileFilter.FilterLines(reader, ["ERROR"], null).ToArray();

        result.Should().Equal(
            Entry("2026-08-11T11:00:01.0000000+09:00", "ERROR", "boom"),
            "System.Exception: boom",
            "   at Mars.Something() in /src/Foo.cs:line 10");
    }

    [Fact]
    public void FilterLines_ByPeriod_DropsOlderEntries()
    {
        var log = string.Join('\n',
            Entry("2026-08-01T09:00:00.0000000+09:00", "WARN", "old"),
            Entry("2026-08-11T11:30:00.0000000+09:00", "WARN", "recent"));

        using var reader = new StringReader(log);
        var result = LogFileFilter.FilterLines(reader, null, new DateTime(2026, 8, 5)).ToArray();

        result.Should().ContainSingle().Which.Should().Contain("recent");
    }

    [Fact]
    public void FilterLines_LegacyFormat_StillParsed()
    {
        var log = string.Join('\n',
            "2026-08-11 11:00:00.000 [1] INFO Test.Logger[0] - noise",
            "2026-08-11 11:00:01.000 [1] ERROR Test.Logger[0] - boom");

        using var reader = new StringReader(log);
        var result = LogFileFilter.FilterLines(reader, ["ERROR"], null).ToArray();

        result.Should().ContainSingle().Which.Should().Contain("boom");
    }

    [Fact]
    public void ReadSeamless_MergesDailyFiles_Chronologically()
    {
        WriteLogFile("2026-08-10",
            Entry(new DateTimeOffset(2026, 8, 10, 10, 0, 0, Tz), "WARN", "day10-first"),
            Entry(new DateTimeOffset(2026, 8, 10, 11, 0, 0, Tz), "WARN", "day10-second"));
        WriteLogFile("2026-08-11",
            Entry(new DateTimeOffset(2026, 8, 11, 12, 0, 0, Tz), "WARN", "day11-first"),
            Entry(new DateTimeOffset(2026, 8, 11, 13, 0, 0, Tz), "ERROR", "day11-second"));

        var result = LogFileFilter.ReadSeamless(dir, null, null, 1000);

        result.Should().HaveCount(4);
        result.First().Should().Contain("day10-first");
        result.Last().Should().Contain("day11-second");
    }

    [Fact]
    public void ReadSeamless_LimitsTotalLines_KeepingNewest()
    {
        WriteLogFile("2026-08-10",
            Entry(new DateTimeOffset(2026, 8, 10, 10, 0, 0, Tz), "WARN", "day10"));
        WriteLogFile("2026-08-11",
            Entry(new DateTimeOffset(2026, 8, 11, 12, 0, 0, Tz), "WARN", "day11-first"),
            Entry(new DateTimeOffset(2026, 8, 11, 13, 0, 0, Tz), "WARN", "day11-second"));

        var result = LogFileFilter.ReadSeamless(dir, null, null, 2);

        result.Should().HaveCount(2);
        result.Should().Contain(l => l.Contains("day11-first"));
        result.Should().Contain(l => l.Contains("day11-second"));
        result.Should().NotContain(l => l.Contains("day10"));
    }

    [Fact]
    public void ReadSeamless_SkipsFilesOlderThanPeriod()
    {
        WriteLogFile("2026-08-10",
            Entry(new DateTimeOffset(2026, 8, 10, 10, 0, 0, Tz), "WARN", "day10"));
        WriteLogFile("2026-08-11",
            Entry(new DateTimeOffset(2026, 8, 11, 12, 0, 0, Tz), "WARN", "day11"));

        var result = LogFileFilter.ReadSeamless(dir, null, new DateTime(2026, 8, 11), 1000);

        result.Should().ContainSingle().Which.Should().Contain("day11");
    }

    [Fact]
    public void ReadSeamless_MissingDir_ReturnsEmpty()
    {
        LogFileFilter.ReadSeamless(Path.Combine(dir, "no-such-dir"), null, null, 100).Should().BeEmpty();
    }

    [Fact]
    public void ParseLevels_NormalizesAliases_ReturnsNullWhenEmpty()
    {
        LogFileFilter.ParseLevels("warn, Error ,critical").Should().BeEquivalentTo(["WARN", "ERROR", "CRITICAL"]);
        LogFileFilter.ParseLevels("warning").Should().BeEquivalentTo(["WARN"]);
        LogFileFilter.ParseLevels("information").Should().BeEquivalentTo(["INFO"]);
        LogFileFilter.ParseLevels("").Should().BeNull();
        LogFileFilter.ParseLevels(null).Should().BeNull();
    }

    [Fact]
    public void DeleteFilesOlderThan_RemovesOnlyOldDatedFiles()
    {
        WriteLogFile("2026-07-01", Entry("2026-07-01T10:00:00.0000000+09:00", "WARN", "old"));
        WriteLogFile("2026-08-10", Entry("2026-08-10T10:00:00.0000000+09:00", "WARN", "kept"));
        File.WriteAllText(Path.Combine(dir, "app_current.log"), "undated");
        File.WriteAllText(Path.Combine(dir, "other_2026-07-01.log"), "not the app pattern");

        var deleted = LogFileFilter.DeleteFilesOlderThan(dir, new DateTime(2026, 8, 1));

        deleted.Should().Be(1);
        File.Exists(Path.Combine(dir, "app_2026-07-01.log")).Should().BeFalse();
        File.Exists(Path.Combine(dir, "app_2026-08-10.log")).Should().BeTrue();
        File.Exists(Path.Combine(dir, "app_current.log")).Should().BeTrue();
        File.Exists(Path.Combine(dir, "other_2026-07-01.log")).Should().BeTrue();
    }

    [Fact]
    public void DeleteFilesOlderThan_MissingDir_ReturnsZero()
    {
        LogFileFilter.DeleteFilesOlderThan(Path.Combine(dir, "no-such-dir"), DateTime.Today).Should().Be(0);
    }

    [Fact]
    public void ParsePeriod_MapsKnownCodes_NullOtherwise()
    {
        LogFileFilter.ParsePeriod("1h").Should().Be(TimeSpan.FromHours(1));
        LogFileFilter.ParsePeriod("6h").Should().Be(TimeSpan.FromHours(6));
        LogFileFilter.ParsePeriod("1d").Should().Be(TimeSpan.FromDays(1));
        LogFileFilter.ParsePeriod("7d").Should().Be(TimeSpan.FromDays(7));
        LogFileFilter.ParsePeriod("30d").Should().Be(TimeSpan.FromDays(30));
        LogFileFilter.ParsePeriod("zzz").Should().BeNull();
        LogFileFilter.ParsePeriod("").Should().BeNull();
    }

    public void Dispose()
    {
        try
        {
            var baseDir = Path.GetDirectoryName(dir)!;
            if (Directory.Exists(baseDir))
                Directory.Delete(baseDir, true);
        }
        catch
        {
            // временная папка
        }
    }
}
