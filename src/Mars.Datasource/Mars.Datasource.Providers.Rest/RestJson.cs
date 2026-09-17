using System.Text.Encodings.Web;
using System.Text.Json;

namespace Mars.Datasource.Providers.Rest;

/// <summary>
/// JSON rest-источника: кириллицу не экранируем — тело запроса и сохранённый каталог читает человек.
/// </summary>
public static class RestJson
{
    public static readonly JsonSerializerOptions Options = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static readonly JsonSerializerOptions Indented = new(Options) { WriteIndented = true };
}
