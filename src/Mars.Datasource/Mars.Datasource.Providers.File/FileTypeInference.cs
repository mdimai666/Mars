namespace Mars.Datasource.Providers.File;

/// <summary>
/// Тип колонки файла по значениям. Имена — из числа понятных <c>FieldTypeMapping</c>
/// (bigint / double precision / boolean / timestamp / uuid / text), поэтому короткое имя типа
/// и подсветка значений в гриде работают для файлов без отдельных правил.
/// </summary>
public static class FileTypeInference
{
    public const string Text = "text";
    public const string BigInt = "bigint";
    public const string Double = "double precision";
    public const string Boolean = "boolean";
    public const string Timestamp = "timestamp";
    public const string Uuid = "uuid";

    public const int SampleRows = 200;

    /// <summary>Пустые значения в выводе не участвуют: колонка с парой заполненных ячеек остаётся числовой.</summary>
    public static string Infer(IEnumerable<string?> values)
    {
        var samples = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Take(SampleRows)
            .ToList();

        if (samples.Count == 0) return Text;
        if (samples.All(value => Val.Num(value) is not null)) return BigInt;
        if (samples.All(value => Val.Dec(value) is not null)) return Double;
        if (samples.All(value => Val.Flag(value) is not null)) return Boolean;
        if (samples.All(value => Val.Id(value) is not null)) return Uuid;
        if (samples.All(value => Val.Date(value) is not null)) return Timestamp;

        return Text;
    }
}
