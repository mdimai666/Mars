using ClosedXML.Excel;
using FluentAssertions;
using Mars.Datasource.Providers.File;

namespace Mars.Datasource.Integration.Tests.FileProviders;

public class XlsxTabularFileReaderTests
{
    readonly XlsxTabularFileReader _reader = new();

    static MemoryStream Workbook(Action<IXLWorkbook> build)
    {
        var stream = new MemoryStream();

        using (var workbook = new XLWorkbook())
        {
            build(workbook);
            workbook.SaveAs(stream);
        }

        stream.Position = 0;

        return stream;
    }

    static MemoryStream Sample() => Workbook(workbook =>
    {
        var sheet = workbook.Worksheets.Add("First");
        sheet.Cell(1, 1).Value = "name";
        sheet.Cell(1, 2).Value = "age";
        sheet.Cell(1, 3).Value = "created";
        sheet.Cell(1, 4).Value = "active";
        sheet.Cell(2, 1).Value = "ann";
        sheet.Cell(2, 2).Value = 35;
        sheet.Cell(2, 3).Value = new DateTime(2024, 3, 1);
        sheet.Cell(2, 4).Value = true;
        sheet.Cell(3, 1).Value = "bob";
        sheet.Cell(3, 3).Value = new DateTime(2024, 3, 1, 10, 20, 30);
        sheet.Cell(3, 4).Value = false;

        var second = workbook.Worksheets.Add("Second");
        second.Cell(1, 1).Value = "id";
        second.Cell(2, 1).Value = 1;
    });

    [Fact]
    public void Read_AllSheets()
    {
        var sheets = _reader.Read(Sample(), new TabularReadOptions());

        sheets.Select(s => s.Name).Should().Equal("First", "Second");
        sheets[0].Columns.Should().Equal("name", "age", "created", "active");
        sheets[0].Rows.Should().HaveCount(2);
        sheets[1].Rows.Single()["id"].Should().Be("1");
    }

    [Fact]
    public void Read_SheetByNameAndByPosition()
    {
        _reader.Read(Sample(), new TabularReadOptions { Sheet = "Second" }).Single().Name.Should().Be("Second");
        _reader.Read(Sample(), new TabularReadOptions { Sheet = "2" }).Single().Name.Should().Be("Second");
        _reader.Read(Sample(), new TabularReadOptions { Sheet = "Nope" }).Should().BeEmpty();
    }

    [Fact]
    public void Read_CellTypesAsInvariantText()
    {
        var sheet = _reader.Read(Sample(), new TabularReadOptions()).First();

        sheet.Rows[0]["name"].Should().Be("ann");
        sheet.Rows[0]["age"].Should().Be("35");
        sheet.Rows[0]["created"].Should().Be("2024-03-01");
        sheet.Rows[0]["active"].Should().Be("true");
        sheet.Rows[1]["created"].Should().Be("2024-03-01T10:20:30");
        sheet.Rows[1]["active"].Should().Be("false");
        // Пустая ячейка — null, а не пустая строка: так же, как NULL из базы.
        sheet.Rows[1]["age"].Should().BeNull();
    }

    [Fact]
    public void Read_MaxRowsStopsAndMarksTruncated()
    {
        var sheet = _reader.Read(Sample(), new TabularReadOptions { MaxRows = 1 }).First();

        sheet.Rows.Should().HaveCount(1);
        sheet.Truncated.Should().BeTrue();
    }

    [Fact]
    public void Read_WithoutHeadersGeneratesColumnNames()
    {
        var sheet = _reader.Read(Sample(), new TabularReadOptions { HasHeaders = false }).First();

        sheet.Columns.Should().Equal("col1", "col2", "col3", "col4");
        sheet.Rows.Should().HaveCount(3);
        sheet.Rows[0]["col1"].Should().Be("name");
    }

    [Fact]
    public void Read_EmptySheetHasNoColumns()
    {
        var sheets = _reader.Read(Workbook(workbook => workbook.Worksheets.Add("Empty")), new TabularReadOptions());

        sheets.Single().Columns.Should().BeEmpty();
        sheets.Single().Rows.Should().BeEmpty();
    }

    [Fact]
    public void CanRead_OnlyXlsx()
    {
        _reader.CanRead("book.xlsx").Should().BeTrue();
        _reader.CanRead("sales.csv").Should().BeFalse();
        // Старый формат Excel не поддерживается: ClosedXML его не читает.
        _reader.CanRead("book.xls").Should().BeFalse();
    }
}
