namespace Mars.Datasource.Providers.File;

/// <summary>Прочитанный лист табличного файла: колонки и строки-словари (значения — текст, как в гриде).</summary>
public class TabularSheet
{
    public required string Name { get; init; }

    public required IReadOnlyList<string> Columns { get; init; }

    public required IReadOnlyList<Dictionary<string, string?>> Rows { get; init; }

    /// <summary>Строк больше, чем позволил лимит чтения.</summary>
    public bool Truncated { get; init; }
}

public class TabularReadOptions
{
    /// <summary>Первая строка — заголовки; иначе колонки называются col1…colN.</summary>
    public bool HasHeaders { get; init; } = true;

    /// <summary>Разделитель CSV; пусто — определить по первой строке.</summary>
    public string? Delimiter { get; init; }

    /// <summary>Лист книги (имя или номер с 1); пусто — все листы.</summary>
    public string? Sheet { get; init; }

    /// <summary>Сколько строк читать; 0 — все.</summary>
    public int MaxRows { get; init; }
}

public interface ITabularFileReader
{
    bool CanRead(string fileName);

    /// <summary>Листы файла: у CSV ровно один, у книги — по листу.</summary>
    IReadOnlyList<TabularSheet> Read(Stream stream, TabularReadOptions options);
}

/// <summary>Общее для ридеров: имена колонок без заголовков, строка-словарь, пустое значение как null.</summary>
public static class TabularFile
{
    public static IReadOnlyList<string> GeneratedColumns(int count)
        => Enumerable.Range(1, Math.Max(count, 0)).Select(index => $"col{index}").ToList();

    /// <summary>Пустые и повторяющиеся имена заголовков не должны терять колонки: имя дополняется номером.</summary>
    public static IReadOnlyList<string> NormalizeHeaders(IReadOnlyList<string?> headers)
    {
        List<string> names = [];

        for (var index = 0; index < headers.Count; index++)
        {
            var name = string.IsNullOrWhiteSpace(headers[index]) ? $"col{index + 1}" : headers[index]!.Trim();

            var duplicate = 1;
            var unique = name;
            while (names.Contains(unique, StringComparer.OrdinalIgnoreCase))
            {
                duplicate++;
                unique = $"{name}_{duplicate}";
            }

            names.Add(unique);
        }

        return names;
    }

    /// <summary>Пустая ячейка — это null, а не пустая строка: так же, как NULL из базы в гриде.</summary>
    public static string? Value(string? raw) => string.IsNullOrEmpty(raw) ? null : raw;

    public static Dictionary<string, string?> Row(IReadOnlyList<string> columns, IReadOnlyList<string?> fields)
    {
        Dictionary<string, string?> row = new(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < columns.Count; index++)
        {
            row[columns[index]] = index < fields.Count ? Value(fields[index]) : null;
        }

        return row;
    }
}
