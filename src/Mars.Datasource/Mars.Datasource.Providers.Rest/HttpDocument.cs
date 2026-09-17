using Mars.Datasource.Abstractions.Models;

namespace Mars.Datasource.Providers.Rest;

/// <summary>
/// Документ запросов в синтаксисе VS Code REST Client (`.http`): перечень адресов, заголовков
/// и тел через разделитель <c>###</c>. Один документ на источник, лежит в
/// <c>data/datasource/&lt;slug&gt;/requests.http</c>.
/// </summary>
public class HttpDocument
{
    public List<HttpDocumentRequest> Requests { get; init; } = [];

    /// <summary>Переменные <c>@name = value</c>, объявленные вне запросов.</summary>
    public Dictionary<string, string> Variables { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public HttpDocumentRequest? ByName(string name)
        => Requests.FirstOrDefault(request => string.Equals(request.Name, name, StringComparison.OrdinalIgnoreCase));
}

/// <summary>Один HTTP-запрос документа.</summary>
public class HttpDocumentRequest
{
    /// <summary>Имя из <c># @name</c> или подпись разделителя <c>###</c>; null — запрос без имени.</summary>
    public string? Name { get; init; }

    public string Method { get; init; } = "GET";

    /// <summary>Адрес как записан: абсолютный, путь относительно адреса API или с <c>{{переменными}}</c>.</summary>
    public string Url { get; init; } = "";

    public List<HttpDocumentHeader> Headers { get; init; } = [];

    /// <summary>Тело запроса; null — запрос без тела.</summary>
    public string? Body { get; init; }

    /// <summary>Переменные, объявленные внутри блока запроса.</summary>
    public Dictionary<string, string> Variables { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Номер строки запроса в документе — для понятных ошибок.</summary>
    public int Line { get; init; }

    /// <summary>Текст блока запроса как он записан в документе — заготовка для редактора.</summary>
    public string Raw { get; init; } = "";

    /// <summary>Метод меняет данные источника: такой запрос выполняется с подтверждением.</summary>
    public bool IsWrite => RestSafety.IsWrite(Method);

    /// <summary>Подпись для дерева и сообщений: <c>GET /wp/v2/posts</c>.</summary>
    public string Title => $"{Method} {Short(Url)}";

    static string Short(string url) => url.Length <= 80 ? url : url[..77] + "…";
}

public record HttpDocumentHeader(string Name, string Value);
