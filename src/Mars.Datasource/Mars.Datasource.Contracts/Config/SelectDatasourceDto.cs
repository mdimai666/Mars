namespace Mars.Datasource.Contracts.Config;

public class SelectDatasourceDto
{
    public string Title { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string Driver { get; set; } = default!;

    /// <summary>Тип источника: по нему выбирается страница запросов (SQL или объекты).</summary>
    public string Kind { get; set; } = DatasourceKind.Sql;

    public SelectDatasourceDto()
    {

    }

    public SelectDatasourceDto(DatasourceConfig config)
    {
        Title = config.Label;
        Slug = config.Slug;
        Driver = config.Driver;
        Kind = config.Kind;
    }
}
