namespace Mars.Datasource.Abstractions.Sql;

public record BackupSettings
{
    public string FilePath { get; init; } = default!;
    public BackupOutputMode Mode { get; init; }
    public DumpMode DumpMode { get; init; }
    public string? DumpBinaryPath { get; init; }
}

public record RestoreSettings
{
    public string FilePath { get; init; } = default!;
    public BackupOutputMode Mode { get; init; }
    public DumpMode DumpMode { get; init; }
}

public enum BackupOutputMode
{
    PlainSql,
    Compressed
}

public enum DumpMode
{
    SchemaAndData,
    Schema,
    DataOnly
}