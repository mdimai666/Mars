using System.Globalization;
using ClosedXML.Excel;

namespace Mars.Datasource.Providers.File;

/// <summary>
/// XLSX через ClosedXML (он уже в стеке на отчётах). Книга грузится в память целиком,
/// поэтому число строк ограничивает <see cref="TabularReadOptions.MaxRows"/>.
/// </summary>
public class XlsxTabularFileReader : ITabularFileReader
{
    public bool CanRead(string fileName)
        => Path.GetExtension(fileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<TabularSheet> Read(Stream stream, TabularReadOptions options)
    {
        using var workbook = new XLWorkbook(stream);

        List<TabularSheet> sheets = [];

        foreach (var worksheet in workbook.Worksheets)
        {
            if (!string.IsNullOrWhiteSpace(options.Sheet) && !Matches(worksheet, options.Sheet)) continue;

            sheets.Add(Read(worksheet, options));

            if (!string.IsNullOrWhiteSpace(options.Sheet)) break;
        }

        return sheets;
    }

    static bool Matches(IXLWorksheet worksheet, string sheet)
        => string.Equals(worksheet.Name, sheet, StringComparison.OrdinalIgnoreCase)
           || (int.TryParse(sheet, out var number) && worksheet.Position == number);

    static TabularSheet Read(IXLWorksheet worksheet, TabularReadOptions options)
    {
        var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        if (lastColumn == 0)
        {
            return new TabularSheet { Name = worksheet.Name, Columns = [], Rows = [] };
        }

        var usedRows = worksheet.RowsUsed().ToList();

        if (usedRows.Count == 0)
        {
            return new TabularSheet { Name = worksheet.Name, Columns = [], Rows = [] };
        }

        var columns = options.HasHeaders
            ? TabularFile.NormalizeHeaders(Enumerable.Range(1, lastColumn).Select(index => Text(usedRows[0].Cell(index))).ToList())
            : TabularFile.GeneratedColumns(lastColumn);

        var dataRows = options.HasHeaders ? usedRows.Skip(1) : usedRows;

        List<Dictionary<string, string?>> rows = [];
        var truncated = false;

        foreach (var row in dataRows)
        {
            if (options.MaxRows > 0 && rows.Count >= options.MaxRows)
            {
                truncated = true;
                break;
            }

            rows.Add(TabularFile.Row(columns, Enumerable.Range(1, lastColumn).Select(index => Text(row.Cell(index))).ToList()));
        }

        return new TabularSheet { Name = worksheet.Name, Columns = columns, Rows = rows, Truncated = truncated };
    }

    /// <summary>Значение ячейки — инвариантным текстом, чтобы его можно было вернуть и сравнить как в SQL-гриде.</summary>
    static string? Text(IXLCell cell)
    {
        var value = cell.Value;

        return value.Type switch
        {
            XLDataType.Blank => null,
            XLDataType.Boolean => value.GetBoolean() ? "true" : "false",
            XLDataType.Number => value.GetNumber().ToString(CultureInfo.InvariantCulture),
            XLDataType.DateTime => Date(value.GetDateTime()),
            XLDataType.TimeSpan => value.GetTimeSpan().ToString("c", CultureInfo.InvariantCulture),
            XLDataType.Text => value.GetText(),
            _ => value.ToString(),
        };
    }

    static string Date(DateTime value)
        => value.TimeOfDay == TimeSpan.Zero
            ? value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
}
