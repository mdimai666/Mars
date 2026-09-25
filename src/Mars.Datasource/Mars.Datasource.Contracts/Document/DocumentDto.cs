namespace Mars.Datasource.Contracts.Document;

/// <summary>
/// Документ запросов источника в data-корне: имя объявляет провайдер
/// (<see cref="DatasourceKindProfile.DocumentName"/>), содержимое правит пользователь.
/// Отдельный тип, а не строка в теле: на строку у ASP.NET Core нет content-type по умолчанию (415).
/// </summary>
public class DocumentDto
{
    public string Name { get; set; } = "";

    public string Content { get; set; } = "";
}
