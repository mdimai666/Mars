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

    /// <summary>Несохранённые правки ячеек: (индекс строки, колонка) → новое значение.</summary>
    public Dictionary<(int Row, string Column), string?> Changes { get; } = [];

    public bool CanEdit => Table is not null && KeyColumns.Count > 0;

    public void Reset()
    {
        Result = null;
        Error = null;
        Changes.Clear();
    }
}
