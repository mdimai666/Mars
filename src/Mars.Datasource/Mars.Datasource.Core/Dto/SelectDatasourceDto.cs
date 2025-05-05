namespace Mars.Datasource.Core.Dto;

public class SelectDatasourceDto
{
    public string Title { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string Driver { get; set; } = default!;

    public string HelpLinkConnectionString => Driver switch
    {
        "psql" => "https://www.npgsql.org/doc/basic-usage.html",
        "mssql" => "https://learn.microsoft.com/en-us/ef/core/",
        "mysql" => "https://dev.mysql.com/doc/connector-net/en/connector-net-connections-string.html",
        _ => ""
    };

    public SelectDatasourceDto()
    {

    }

    public SelectDatasourceDto(DatasourceConfig config)
    {
        this.Title = config.Label;
        this.Slug = config.Slug;
        this.Driver = config.Driver;
    }

    public char EscapeQuotationMark => Driver switch
    {
        "psql" => '\"',
        "mssql" => '\"',
        "mysql" => '`',
        _ => '\"'
    };
}
