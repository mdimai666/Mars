namespace Mars.Datasource.Contracts.Models;

/// <summary>Что источник умеет: по этим флагам UI показывает или прячет действия.</summary>
public class DatasourceCapabilities
{
    /// <summary>Есть язык запроса (SQL, LINQ) — показывать редактор.</summary>
    public bool CanQuery { get; set; }

    /// <summary>Объект каталога можно открыть и посмотреть строки.</summary>
    public bool CanBrowse { get; set; }

    /// <summary>Источник можно писать (UPDATE/INSERT, правка ячеек).</summary>
    public bool CanWrite { get; set; }

    /// <summary>DDL вьюх: создание, замена, удаление, определение.</summary>
    public bool CanManageViews { get; set; }
}
