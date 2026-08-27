namespace Mars.Host.Shared.Services;

public interface IMetaEntityTypeProvider
{
    Task<IReadOnlyCollection<MtoModelInfo>> GenerateMetaTypes();
    Task<string> GenerateMetaTypesSourceCode();
}

public record MtoModelInfo
{
    public required Type CreatedType { get; init; }
    public required Type BaseEntityType { get; init; }
    public required string KeyName { get; set; }
}
