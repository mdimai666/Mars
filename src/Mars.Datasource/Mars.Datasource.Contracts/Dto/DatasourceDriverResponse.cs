namespace Mars.Datasource.Contracts.Dto;

/// <summary>Провайдер источника для формы настроек: тип источника, ключ драйвера, подсказка и ссылка на док.</summary>
public record DatasourceDriverResponse
{
    public required string Kind { get; init; }
    public required string Driver { get; init; }
    public required string DisplayName { get; init; }
    public required string DefaultConnectionString { get; init; }
    public required string HelpLink { get; init; }
}
