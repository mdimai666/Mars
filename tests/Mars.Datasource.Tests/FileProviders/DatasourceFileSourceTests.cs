using FluentAssertions;
using Mars.Datasource.Host.Services;
using Mars.Storage.Services;

namespace Mars.Datasource.Tests.FileProviders;

/// <summary>
/// Ссылка на файл источника: относительный путь — медиа-хранилище Mars, корневой — диск хоста.
/// В data-корень файлы не копируются (решение 2026-09-16).
/// </summary>
public class DatasourceFileSourceTests
{
    [Fact]
    public void RelativeReference_ReadsFromMediaStorage()
    {
        var media = new InMemoryFileStorage(new Dictionary<string, string> { ["2026/09/sales.csv"] = "a\n1\n" });
        var source = new DatasourceFileSource(media);

        source.Exists("2026/09/sales.csv").Should().BeTrue();
        source.Exists("2026/09/other.csv").Should().BeFalse();

        using var stream = new StreamReader(source.OpenRead("2026/09/sales.csv"));
        stream.ReadToEnd().Should().Be("a\n1\n");
    }

    [Fact]
    public void RelativeReference_NormalizesBackslashes()
    {
        var media = new InMemoryFileStorage(new Dictionary<string, string> { ["2026/09/sales.csv"] = "a\n1\n" });
        var source = new DatasourceFileSource(media);

        source.Exists(@"2026\09\sales.csv").Should().BeTrue();
    }

    [Fact]
    public void RootedReference_FallsBackToMediaWhenHostFileMissing()
    {
        // Путь копируют из медиа, а там он бывает с ведущим слэшем.
        var media = new InMemoryFileStorage(new Dictionary<string, string> { ["2026/09/sales.csv"] = "a\n1\n" });
        var source = new DatasourceFileSource(media);

        source.Exists("/2026/09/sales.csv").Should().BeTrue();

        using var reader = new StreamReader(source.OpenRead("/2026/09/sales.csv"));
        reader.ReadToEnd().Should().Be("a\n1\n");
    }

    [Fact]
    public void RootedReference_ReadsFromHostDisk()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mars-ds-{Guid.NewGuid():N}.csv");
        File.WriteAllText(path, "a,b\n1,2\n");

        try
        {
            var source = new DatasourceFileSource(new InMemoryFileStorage());

            source.Exists(path).Should().BeTrue();

            using var reader = new StreamReader(source.OpenRead(path));
            reader.ReadToEnd().Should().Be("a,b\n1,2\n");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RootedReference_MissingFileIsNotExists()
    {
        var source = new DatasourceFileSource(new InMemoryFileStorage());

        source.Exists(Path.Combine(Path.GetTempPath(), "mars-ds-missing.csv")).Should().BeFalse();
    }
}
