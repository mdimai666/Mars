namespace Mars.Datasource.Contracts.Models;

public class SelectDatasourceDto
{
    public string Title { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string Driver { get; set; } = default!;

    public SelectDatasourceDto()
    {

    }

    public SelectDatasourceDto(DatasourceConfig config)
    {
        Title = config.Label;
        Slug = config.Slug;
        Driver = config.Driver;
    }
}
