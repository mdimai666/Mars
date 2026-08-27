namespace Mars.Host.Data.Options;

public sealed record DatabaseConnectionOpt
{
    public required string ConnectionString { get; init; }
    public required string ProviderName { get; init; }

}
