namespace Mars.Datasource.Contracts.Dto;

/// <summary>Провайдер источника для формы настроек: ключ движка, подсказка строки подключения и ссылка на док.</summary>
public record DatasourceDriverResponse
{
    public required string Driver { get; init; }
    public required string DefaultConnectionString { get; init; }
    public required string HelpLink { get; init; }
}
