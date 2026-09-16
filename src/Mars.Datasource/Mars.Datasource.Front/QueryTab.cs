using Mars.Datasource.Contracts.Models;

namespace Mars.Datasource.Front;

/// <summary>Вкладка запроса: свой текст запроса, свой результат и свои несохранённые правки.</summary>
public class QueryTab
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "query";

    /// <summary>Текст редактора: SQL у базы, предикат Dynamic LINQ у файла.</summary>
    public string Text { get; set; } = "";

    /// <summary>Язык текста: из открытого объекта, иначе из типа источника.</summary>
    public string Language { get; set; } = DatasourceLanguage.Sql;

    /// <summary>Открытый объект каталога (просмотр данных).</summary>
    public DatasourceCatalogObject? Object { get; set; }

    /// <summary>Группа открытого объекта: схема у sql, пусто у файла.</summary>
    public string Schema { get; set; } = "";

    /// <summary>Колонки первичного ключа открытой таблицы — без них правка выключена.</summary>
    public List<string> KeyColumns { get; set; } = [];

    /// <summary>Источник вообще можно писать (`DatasourceCapabilities.CanWrite`).</summary>
    public bool SourceWritable { get; set; }

    public QueryResultDto? Result { get; set; }
    public bool Loading { get; set; }
    public bool ShowJson { get; set; }
    public string? Error { get; set; }
    public int MaxRows { get; set; } = 500;

    /// <summary>Сколько строк показывать при просмотре объекта из дерева (в самом SQL, а не только на сервере).</summary>
    public int BrowseLimit { get; set; } = 50;

    /// <summary>SQL просмотра, который сгенерировали мы; null — вкладка не про просмотр SQL-объекта.</summary>
    public string? BrowseSql { get; set; }

    /// <summary>Общее число строк объекта: известно только для нашего просмотра (см. <see cref="BrowseSql"/>).</summary>
    public long? Total { get; set; }

    /// <summary>Подпись к количеству строк: «считаем всего…» / «всего не сосчитали».</summary>
    public string? TotalNote { get; set; }

    /// <summary>Вкладка показывает объект из дерева, и её текст с тех пор не правили руками.</summary>
    public bool IsBrowse => BrowseSql is not null && BrowseSql == Text;

    /// <summary>Есть смысл просить больше строк: упёрлись либо в лимит просмотра, либо в серверный.</summary>
    public bool CanLoadMore => Result?.Ok == true
        && (IsBrowse ? Result.Rows.Length >= BrowseLimit : Result.Truncated);

    /// <summary>Несохранённые правки ячеек: (индекс строки, колонка) → новое значение.</summary>
    public Dictionary<(int Row, string Column), string?> Changes { get; } = [];

    public bool CanEdit => Object is not null && SourceWritable && KeyColumns.Count > 0;

    /// <summary>Почему правка ячеек выключена; null — правка доступна или объект не открыт.</summary>
    public string? EditDisabledReason => Object is null || CanEdit
        ? null
        : SourceWritable
            ? "правка недоступна: у объекта нет первичного ключа"
            : "источник только для чтения";

    public void Reset()
    {
        Result = null;
        Error = null;
        Total = null;
        TotalNote = null;
        Changes.Clear();
    }
}
