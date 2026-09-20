using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace Mars.Datasource.Providers.File;

/// <summary>CSV встроенным парсером .NET: кавычки, разделители внутри значений и BOM он берёт на себя.</summary>
public class CsvTabularFileReader : ITabularFileReader
{
    static readonly string[] DetectableDelimiters = [",", ";", "\t", "|"];

    public bool CanRead(string fileName)
        => Path.GetExtension(fileName).Equals(".csv", StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<TabularSheet> Read(Stream stream, TabularReadOptions options)
    {
        var delimiter = ResolveDelimiter(stream, options.Delimiter);

        using var parser = new TextFieldParser(stream, Encoding.UTF8, detectEncoding: true, leaveOpen: true)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false,
        };

        parser.SetDelimiters(delimiter);

        if (parser.EndOfData)
        {
            return [new TabularSheet { Name = "", Columns = [], Rows = [] }];
        }

        var firstLine = parser.ReadFields() ?? [];

        var columns = options.HasHeaders
            ? TabularFile.NormalizeHeaders(firstLine)
            : TabularFile.GeneratedColumns(firstLine.Length);

        List<Dictionary<string, string?>> rows = [];
        var truncated = false;

        if (!options.HasHeaders)
        {
            rows.Add(TabularFile.Row(columns, firstLine));
        }

        while (!parser.EndOfData)
        {
            if (options.MaxRows > 0 && rows.Count >= options.MaxRows)
            {
                truncated = true;
                break;
            }

            rows.Add(TabularFile.Row(columns, parser.ReadFields() ?? []));
        }

        return [new TabularSheet { Name = "", Columns = columns, Rows = rows, Truncated = truncated }];
    }

    /// <summary>Разделитель из настройки (`;`, `tab`, `|`) или по первой строке, считая символы вне кавычек.</summary>
    static string ResolveDelimiter(Stream stream, string? configured)
    {
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.Trim().ToLowerInvariant() switch
            {
                "tab" or "\\t" => "\t",
                "comma" => ",",
                "semicolon" => ";",
                "pipe" => "|",
                _ => configured,
            };
        }

        if (!stream.CanSeek) return ",";

        var position = stream.Position;

        try
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            return Detect(reader.ReadLine() ?? "");
        }
        finally
        {
            stream.Position = position;
        }
    }

    static string Detect(string line)
    {
        var best = ",";
        var bestCount = 0;
        var inQuotes = false;

        foreach (var delimiter in DetectableDelimiters)
        {
            var count = 0;

            foreach (var symbol in line)
            {
                if (symbol == '"') inQuotes = !inQuotes;
                else if (!inQuotes && symbol.ToString() == delimiter) count++;
            }

            inQuotes = false;

            if (count > bestCount)
            {
                bestCount = count;
                best = delimiter;
            }
        }

        return best;
    }
}
