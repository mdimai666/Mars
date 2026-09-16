using System.Text;
using FluentAssertions;
using Mars.Datasource.Providers.File;

namespace Mars.Datasource.Integration.Tests.FileProviders;

public class CsvTabularFileReaderTests
{
    readonly CsvTabularFileReader _reader = new();

    static MemoryStream Stream(string content, bool withBom = false)
    {
        var bytes = withBom
            ? [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(content)]
            : Encoding.UTF8.GetBytes(content);

        return new MemoryStream(bytes);
    }

    TabularSheet Read(string content, TabularReadOptions? options = null)
        => _reader.Read(Stream(content), options ?? new TabularReadOptions()).Single();

    [Fact]
    public void Read_QuotedFieldsAndEmptyValues()
    {
        var content = "name,note,age\n"
                      + "\"ann, a\",\"he said \"\"hi\"\"\",35\n"
                      + "bob,,\n";

        var sheet = Read(content);

        sheet.Columns.Should().Equal("name", "note", "age");
        sheet.Rows.Should().HaveCount(2);
        sheet.Rows[0]["name"].Should().Be("ann, a");
        sheet.Rows[0]["note"].Should().Be("he said \"hi\"");
        sheet.Rows[0]["age"].Should().Be("35");
        sheet.Rows[1]["note"].Should().BeNull();
        sheet.Rows[1]["age"].Should().BeNull();
    }

    [Fact]
    public void Read_DetectsSemicolonDelimiter()
    {
        var sheet = Read("a;b;c\n1;2;3\n");

        sheet.Columns.Should().Equal("a", "b", "c");
        sheet.Rows.Single()["b"].Should().Be("2");
    }

    [Fact]
    public void Read_ExplicitDelimiterFromSettings()
    {
        var sheet = Read("a\tb\n1\t2\n", new TabularReadOptions { Delimiter = "tab" });

        sheet.Columns.Should().Equal("a", "b");
    }

    [Fact]
    public void Read_WithoutHeaders_FirstLineIsData()
    {
        var sheet = Read("1,2\n3,4\n", new TabularReadOptions { HasHeaders = false });

        sheet.Columns.Should().Equal("col1", "col2");
        sheet.Rows.Should().HaveCount(2);
        sheet.Rows[0]["col1"].Should().Be("1");
        sheet.Rows[1]["col2"].Should().Be("4");
    }

    [Fact]
    public void Read_BomDoesNotLeakIntoColumnName()
    {
        var sheet = _reader.Read(Stream("a,b\n1,2\n", withBom: true), new TabularReadOptions()).Single();

        sheet.Columns.Should().Equal("a", "b");
    }

    [Fact]
    public void Read_DuplicateHeadersGetSuffix()
    {
        var sheet = Read("a,a\n1,2\n");

        sheet.Columns.Should().Equal("a", "a_2");
        sheet.Rows.Single()["a_2"].Should().Be("2");
    }

    [Fact]
    public void Read_MaxRowsStopsAndMarksTruncated()
    {
        var sheet = Read("a\n1\n2\n3\n4\n5\n", new TabularReadOptions { MaxRows = 2 });

        sheet.Rows.Should().HaveCount(2);
        sheet.Truncated.Should().BeTrue();
    }

    [Fact]
    public void Read_EmptyStreamGivesEmptySheet()
    {
        var sheet = _reader.Read(new MemoryStream(), new TabularReadOptions()).Single();

        sheet.Columns.Should().BeEmpty();
        sheet.Rows.Should().BeEmpty();
        sheet.Truncated.Should().BeFalse();
    }

    [Fact]
    public void Read_ExtraFieldsBeyondHeadersAreIgnored()
    {
        var sheet = Read("a,b\n1,2,3\n");

        sheet.Columns.Should().Equal("a", "b");
        sheet.Rows.Single().Should().HaveCount(2);
    }

    [Fact]
    public void CanRead_OnlyCsv()
    {
        _reader.CanRead("sales.csv").Should().BeTrue();
        _reader.CanRead("SALES.CSV").Should().BeTrue();
        _reader.CanRead("book.xlsx").Should().BeFalse();
    }
}
