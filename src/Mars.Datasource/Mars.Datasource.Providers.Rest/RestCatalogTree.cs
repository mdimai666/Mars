using Mars.Datasource.Abstractions.Models;
using Mars.Datasource.Contracts.Models;

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
    /// Метод операции: у discovery он в идентификаторе (<c>GET /wp/v2/posts</c>),
    /// у запроса из документа — в его тексте (<c>POST {{baseUrl}}/…</c>).
    /// </summary>
    static string MethodOf(DatasourceCatalogObject obj)
    {
        var fromId = Known(obj.Id);

        if (fromId.Length > 0) return fromId;

        var fromText = Known(obj.DefaultQuery);

        return fromText.Length > 0 ? fromText : OtherMethods;
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
        Columns = obj.Columns,
        Parameters = obj.Parameters,
        DefaultLanguage = obj.DefaultLanguage,
        DefaultQuery = obj.DefaultQuery,
        Line = obj.Line,
        EndLine = obj.EndLine,
    };

    static string StripMethod(string name, string method)
        => name.StartsWith(method + " ", StringComparison.OrdinalIgnoreCase) ? name[(method.Length + 1)..] : name;

    static int Order(string method)
    {
        var index = Array.FindIndex(MethodOrder, item => string.Equals(item, method, StringComparison.OrdinalIgnoreCase));

        return index < 0 ? int.MaxValue : index;
    }
}
