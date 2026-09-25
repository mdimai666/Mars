using System.Text;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.Datasource.Contracts.Sql;

namespace Mars.Datasource.Contracts.Ai;

/// <summary>
/// Каталог источника в компактный текст для AI-агента: объекты, поля, параметры операций.
/// Чистая функция — маппинг тестируется без провайдеров и источников.
/// </summary>
public static class DatasourceSchemaText
{
    /// <summary>
    /// Каталог → текст. <paramref name="filter"/> — подстрока для отбора объектов по id/имени,
    /// <paramref name="maxChars"/> — бюджет вывода: переполнение обрезает список объектов
    /// с подсказкой сузить фильтр.
    /// </summary>
    public static string Build(DatasourceCatalog catalog, string? filter, int maxChars)
    {
        var objects = catalog.Groups.SelectMany(group => group.Objects).ToList();

        var selected = string.IsNullOrWhiteSpace(filter)
            ? objects
            : objects.Where(item => Contains(item.Id, filter) || Contains(item.Name, filter)).ToList();

        var text = new StringBuilder();

        text.Append("Источник: ").Append(catalog.SourceName)
            .Append(" · kind=").Append(catalog.Profile.Kind);

        if (!string.IsNullOrEmpty(catalog.Profile.Driver))
            text.Append(" · driver=").Append(catalog.Profile.Driver);

        text.AppendLine();

        text.Append("Объектов: ").Append(selected.Count);
        if (selected.Count != objects.Count)
            text.Append($" (из {objects.Count}, фильтр \"{filter.Trim()}\")");
        text.AppendLine().AppendLine();

        var shown = 0;
        var truncated = false;

        foreach (var obj in selected)
        {
            var entry = Render(obj);

            if (text.Length + entry.Length > maxChars)
            {
                truncated = true;
                break;
            }

            text.Append(entry);
            shown++;
        }

        if (truncated)
            text.AppendLine($"… показаны {shown} из {selected.Count} объектов — каталог большой, сузи фильтр и вызови снова.");

        return text.ToString().TrimEnd();
    }

    static string Render(DatasourceCatalogObject obj)
    {
        var text = new StringBuilder("- ").Append(obj.Id);

        if (obj.Operation is not null)
        {
            text.Append(" — операция");
            if (RestSafety.IsWrite(obj.Operation.Method)) text.Append(" (запись)");
            text.AppendLine();

            foreach (var parameter in obj.Operation.Parameters)
                text.Append("    ").AppendLine(RenderParameter(parameter));

            return text.ToString();
        }

        text.Append(" (").Append(obj.ObjectType).Append(')');

        if (obj.Fields.Count > 0)
            text.Append(": ").Append(string.Join(", ", obj.Fields.Select(RenderField)));

        return text.AppendLine().ToString();
    }

    static string RenderField(DatasourceField field)
        => (field.IsKey ? "*" : "") + field.Name +
           (string.IsNullOrEmpty(field.DataTypeName) ? "" : ":" + field.DataTypeName);

    static string RenderParameter(DatasourceOperationParameter parameter)
    {
        var text = new StringBuilder(parameter.Name)
            .Append('(').Append(parameter.In)
            .Append(',').Append(parameter.Type);

        if (parameter.Required) text.Append(",обязательный");
        if (parameter.Enum is { Count: > 0 }) text.Append(",один из: ").Append(string.Join('|', parameter.Enum));
        if (!string.IsNullOrEmpty(parameter.Default)) text.Append(",по умолчанию: ").Append(parameter.Default);

        text.Append(')');

        if (!string.IsNullOrWhiteSpace(parameter.Description))
            text.Append(" — ").Append(parameter.Description);

        return text.ToString();
    }

    static bool Contains(string? value, string filter)
        => value?.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase) == true;
}
