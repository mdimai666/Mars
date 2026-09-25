namespace Mars.Datasource.Contracts.Catalog;

/// <summary>
/// Поле источника: колонка таблицы, лист файла, поле ответа API. Один тип и для каталога
/// (что известно до запроса), и для результата запроса (что пришло из схемы результата).
/// </summary>
public class DatasourceField
{
    public string Name { get; set; } = "";

    /// <summary>Порядковый номер в объекте; 0 — источник порядок не сообщил.</summary>
    public int Ordinal { get; set; }

    /// <summary>Имя типа источника (`uuid`, `jsonb`, `varchar`, `bigint`) — по нему определяется категория значения.</summary>
    public string DataTypeName { get; set; } = "";

    public string ClrTypeName { get; set; } = "";

    /// <summary>Размер из каталога: у MySQL longtext/JSON он больше int (4294967295).</summary>
    public long? Size { get; set; }

    public bool IsNullable { get; set; } = true;

    /// <summary>Поле входит в первичный ключ: без него правка значения невозможна.</summary>
    public bool IsKey { get; set; }

    public bool IsJson { get; set; }

    public bool IsAutoIncrement { get; set; }
}
