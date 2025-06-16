namespace Mars.Host.Shared.Services;

public interface IMetaEntityTypeProvider
{
    Task<Dictionary<string, Type>> GenerateMetaTypes();
    Task<string> GenerateMetaTypesSourceCode();
}
