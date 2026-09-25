using System.Text.Json;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;

namespace Mars.Datasource.Contracts.Ai;

/// <summary>
/// Параметры операции каталога из JSON-текста «объект имя→значение» — формат, в котором их
/// передаёт AI-агент (<c>parametersJson</c> в run_query). Чистая функция: тестируется без провайдеров.
/// </summary>
public static class DatasourceParametersJson
{
    /// <summary>Пустой текст — параметров нет (null); не-объект — <see cref="FormatException"/>.</summary>
    public static List<DatasourceParam>? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        using var document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new FormatException("ожидается JSON-объект «имя\":\"значение»");

        List<DatasourceParam> parameters = [];

        foreach (var property in document.RootElement.EnumerateObject())
        {
            parameters.Add(new DatasourceParam
            {
                Name = property.Name,
                Value = property.Value.ValueKind switch
                {
                    JsonValueKind.Null or JsonValueKind.Undefined => null,
                    JsonValueKind.String => property.Value.GetString(),
                    _ => property.Value.GetRawText(),
                },
            });
        }

        return parameters;
    }
}
