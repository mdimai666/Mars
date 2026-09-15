using Mars.Datasource.Abstractions.Models;
using Mars.Datasource.Dto;

namespace Mars.Datasource.Front;

/// <summary>Вкладка запроса: свой SQL, свой результат и свои несохранённые правки.</summary>
public class QueryTab
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "query";
    public string Sql { get; set; } = "";

    /// <summary>Открытая таблица (просмотр данных) — включает правку ячеек.</summary>
    public QTableResponse? Table { get; set; }

    /// <summary>Колонки первичного ключа открытой таблицы — без них правка выключена.</summary>
    public List<string> KeyColumns { get; set; } = [];

    public QueryResultDto? Result { get; set; }
    public bool Loading { get; set; }
    public bool ShowJson { get; set; }
    public string? Error { get; set; }
    public int MaxRows { get; set; } = 500;

    /// <summary>Сколько строк показывать при просмотре объекта из дерева (в самом SQL, а не только на сервере).</summary>
    public int BrowseLimit { get; set; } = 50;

    /// <summary>SQL просмотра, который сгенерировали мы; null — вкладка не про объект из дерева.</summary>
    public string? BrowseSql { get; set; }

    /// <summary>Общее число строк объекта: известно только для нашего просмотра (см. <see cref="BrowseSql"/>).</summary>
    public long? Total { get; set; }

    /// <summary>Подпись к количеству строк: «считаем всего…» / «всего не сосчитали».</summary>
    public string? TotalNote { get; set; }

    /// <summary>Вкладка показывает объект из дерева, и её SQL с тех пор не правили руками.</summary>
    public bool IsBrowse => BrowseSql is not null && BrowseSql == Sql;

    /// <summary>Есть смысл просить больше строк: упёрлись либо в лимит просмотра, либо в серверный.</summary>
    public bool CanLoadMore => Result?.Ok == true
        && (IsBrowse ? Result.Rows.Length >= BrowseLimit : Result.Truncated);

    /// <summary>Несохранённые правки ячеек: (индекс строки, колонка) → новое значение.</summary>
    public Dictionary<(int Row, string Column), string?> Changes { get; } = [];

    public bool CanEdit => Table is not null && KeyColumns.Count > 0;

    public void Reset()
    {
        Result = null;
        Error = null;
        Total = null;
        TotalNote = null;
        Changes.Clear();
    }
}
