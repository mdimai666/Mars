using Mars.Datasource.Contracts.Sql;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.Datasource.Front.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Mars.Datasource.Front.Components;

/// <summary>
/// Просьба открыть правку ячейки: грид решает, править её инлайном или в модалке длинного значения.
/// </summary>
public record CellEditRequest(int RowIndex, string Column, string? Value, FieldKind Kind);

/// <summary>
/// Таблица результата: строки, ячейки, раскраска по типу, сжатие guid и json-значения.
/// Правка — только если её включил вызывающий: сама таблица значения не хранит, а сообщает о запросах.
/// </summary>
public partial class ResultTable : ComponentBase
{
    [Inject] MarsDatasourceFrontJsInterop jsInterop { get; set; } = default!;

    [Parameter, EditorRequired] public QueryResultDto Result { get; set; } = default!;

    /// <summary>Несохранённые правки ячеек: их показывает таблица вместо значений результата.</summary>
    [Parameter] public IReadOnlyDictionary<(int Row, string Column), string?>? Changes { get; set; }

    [Parameter] public bool CanEdit { get; set; }
    [Parameter] public (int Row, string Column)? EditCell { get; set; }
    [Parameter] public string? EditValue { get; set; }
    [Parameter] public EventCallback<string?> EditValueChanged { get; set; }
    [Parameter] public EventCallback<CellEditRequest> OnStartEdit { get; set; }
    [Parameter] public EventCallback OnCommitEdit { get; set; }
    [Parameter] public EventCallback<KeyboardEventArgs> OnEditKeyDown { get; set; }

    /// <summary>Длина начальных и конечных символов, которые видны у сжатого значения.</summary>
    const int MidHeadLength = 8;
    const int MidTailLength = 6;

    ElementReference _root;

    QueryResultDto? _guidDetectedFor;
    bool[] _guidColumns = [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Копирование выделения из грида отдаёт полные значения ячеек, а не «начало…конец».
            await jsInterop.RegisterFullValueCopy(_root);
        }
    }

    /// <summary>
    /// Класс цвета по категории типа — для значения ячейки и для подписи типа.
    /// Строковые без класса: их большинство, и в ячейке это основной текст (цвет по умолчанию), а не подпись.
    /// </summary>
    static string? KindClass(FieldKind kind)
        => kind switch
        {
            FieldKind.Number => "ds-type-number",
            FieldKind.Boolean => "ds-type-bool",
            FieldKind.DateTime => "ds-type-date",
            FieldKind.Json => "ds-type-json",
            _ => null,
        };

    /// <summary>
    /// Начало и конец длинного значения без середины — для колонок с guid-ами: 36 символов
    /// распирают таблицу, а по краям значение всё ещё узнаваемо. `null` — показываем целиком.
    /// </summary>
    (string Head, string Tail)? MidParts(int columnIndex, string? value)
    {
        if (value is null || !IsGuidColumn(columnIndex)) return null;
        if (value.Length <= MidHeadLength + MidTailLength + 1) return null;

        return (value[..MidHeadLength], value[^MidTailLength..]);
    }

    bool IsGuidColumn(int columnIndex)
    {
        if (!ReferenceEquals(_guidDetectedFor, Result))
        {
            _guidDetectedFor = Result;
            _guidColumns = DetectGuidColumns();
        }

        return columnIndex >= 0 && columnIndex < _guidColumns.Length && _guidColumns[columnIndex];
    }

    bool[] DetectGuidColumns()
    {
        var flags = new bool[Result.Fields.Length];

        for (var i = 0; i < flags.Length; i++)
        {
            flags[i] = FieldTypeMapping.Kind(Result.Fields[i].DataTypeName) == FieldKind.Guid
                || LooksLikeGuidColumn(i);
        }

        return flags;
    }

    /// <summary>Guid в char-колонке (MySQL, старые схемы) по типу не отличить — смотрим значения.</summary>
    bool LooksLikeGuidColumn(int columnIndex)
    {
        const int sampleSize = 5;

        var sampled = 0;
        var guids = 0;

        foreach (var row in Result.Rows)
        {
            if (sampled >= sampleSize) break;
            if (columnIndex >= row.Length) continue;

            var value = row[columnIndex];
            if (string.IsNullOrEmpty(value)) continue;

            sampled++;

            if (Guid.TryParse(value, out _)) guids++;
        }

        return sampled > 0 && guids == sampled;
    }

    bool IsEditing((int Row, string Column) cell) => CanEdit && EditCell == cell;
}
