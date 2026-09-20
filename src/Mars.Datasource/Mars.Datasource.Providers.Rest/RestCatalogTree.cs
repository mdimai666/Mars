using Mars.Datasource.Abstractions.Mappings;
using Mars.Datasource.Abstractions.Sql;
using Mars.Datasource.Contracts.Sql;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;

namespace Mars.Datasource.Providers.Rest;

/// <summary>
/// Дерево каталога rest-источника: операции раскладываются по методу запроса. Так их ищут в списке
/// («где POST»), тогда как имя группы из описания API (namespace WordPress, тег OpenAPI) для этого
/// ничего не даёт. Строка внутри группы — путь без метода или имя запроса из документа пользователя.
/// </summary>
public static class RestCatalogTree
{
    /// <summary>Группа операций, у которых метод не определился (например, сломанный запрос документа).</summary>
    public const string OtherMethods = "прочее";

    static readonly string[] MethodOrder = ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"];

    public static List<DatasourceCatalogGroup> ByMethod(IEnumerable<DatasourceCatalogGroup> groups)
    {
        Dictionary<string, DatasourceCatalogGroup> methods = new(StringComparer.OrdinalIgnoreCase);

        foreach (var obj in groups.SelectMany(group => group.Objects))
        {
            var method = MethodOf(obj);

            if (!methods.TryGetValue(method, out var group))
            {
                group = new DatasourceCatalogGroup { Name = method };
                methods[method] = group;
            }

            group.Objects.Add(Row(obj, method));
        }

        return methods.Values
            .OrderBy(group => Order(group.Name))
            .Select(group => new DatasourceCatalogGroup
            {
                Name = group.Name,
                Objects = group.Objects.OrderBy(obj => obj.Name, StringComparer.OrdinalIgnoreCase).ToList(),
            })
            .ToList();
    }

    /// <summary>
    /// Метод операции: у discovery и у запроса из документа он известен из каталога,
    /// а если операция собрана без него — берём из текста (<c>POST {{baseUrl}}/…</c>).
    /// </summary>
    static string MethodOf(DatasourceCatalogObject obj)
    {
        var known = Known(obj.Operation?.Method);

        if (known.Length > 0) return known;

        var fromId = Known(obj.Id);

        if (fromId.Length > 0) return fromId;

        return Known(obj.DefaultQuery) is { Length: > 0 } fromText ? fromText : OtherMethods;
    }

    static string Known(string? text)
    {
        var method = RestSafety.FirstMethod(text);

        return MethodOrder.Contains(method, StringComparer.OrdinalIgnoreCase) ? method : "";
    }

    /// <summary>Строка группы: метод уже в её имени, в самой строке он только мешал бы.</summary>
    static DatasourceCatalogObject Row(DatasourceCatalogObject obj, string method) => new()
    {
        Id = obj.Id,
        Name = StripMethod(obj.Name, method),
        ObjectType = obj.ObjectType,
        Fields = obj.Fields,
        Operation = obj.Operation,
        DefaultLanguage = obj.DefaultLanguage,
        DefaultQuery = obj.DefaultQuery,
    };

    static string StripMethod(string name, string method)
        => name.StartsWith(method + " ", StringComparison.OrdinalIgnoreCase) ? name[(method.Length + 1)..] : name;

    static int Order(string method)
    {
        var index = Array.FindIndex(MethodOrder, item => string.Equals(item, method, StringComparison.OrdinalIgnoreCase));

        return index < 0 ? int.MaxValue : index;
    }
}
