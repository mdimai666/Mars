using Mars.Host.Shared.Dto.PostCategoryTypes;

namespace Mars.Host.Shared.Services;

public interface IPostCategoryMetaLocator
{
    PostCategoryTypeDetail? GetTypeDetailById(Guid id);
    PostCategoryTypeDetail? GetTypeDetailByName(string postCategoryTypeName);
    IReadOnlyDictionary<string, PostCategoryTypeDetail> GetTypeDict();
    void InvalidateCompiledMetaMtoModels();
    bool ExistType(Guid id);
    bool ExistType(string postCategoryTypeName);
}
