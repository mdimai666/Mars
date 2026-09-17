namespace Mars.Datasource.Contracts.Models;

/// <summary>
/// Тело запроса на сохранение документа запросов источника: строка в теле требует
/// content-type, который ASP.NET Core по умолчанию не разбирает, поэтому передаём объектом.
/// </summary>
public class RequestsDocumentDto
{
    public string Content { get; set; } = "";
}
