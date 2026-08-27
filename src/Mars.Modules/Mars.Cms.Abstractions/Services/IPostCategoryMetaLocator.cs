using Mars.Cms.Abstractions.Dto.PostCategoryTypes;

namespace Mars.Cms.Abstractions.Services;

public interface IPostCategoryMetaLocator
{
    PostCategoryTypeDetail? GetTypeDetailById(Guid id);
    PostCategoryTypeDetail? GetTypeDetailByName(string postCategoryTypeName);
    IReadOnlyDictionary<string, PostCategoryTypeDetail> GetTypeDict();
    void InvalidateCache();
    bool ExistType(Guid id);
    bool ExistType(string postCategoryTypeName);
}
