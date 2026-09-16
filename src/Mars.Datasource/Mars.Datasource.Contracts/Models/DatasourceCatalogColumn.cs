namespace Mars.Datasource.Contracts.Models;

/// <summary>Колонка объекта каталога. Полнее, чем <see cref="QueryColumn"/> результата запроса.</summary>
public class DatasourceCatalogColumn
{
    public string Name { get; set; } = "";

    public int Ordinal { get; set; }

    public string DataTypeName { get; set; } = "";

    public string ClrTypeName { get; set; } = "";

    /// <summary>Размер из каталога: у MySQL longtext/JSON он больше int (4294967295).</summary>
    public long? Size { get; set; }

    public bool IsNullable { get; set; } = true;

    public bool IsKey { get; set; }

    public bool IsJson { get; set; }

    public bool IsAutoIncrement { get; set; }
}
