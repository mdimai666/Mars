using Mars.Host.Shared.Dto.UserTypes;

namespace Mars.Host.Shared.Services;

public interface IUserMetaLocator
{
    UserTypeDetail? GetTypeDetailById(Guid id);
    UserTypeDetail? GetTypeDetailByName(string userTypeName);
    IReadOnlyDictionary<string, UserTypeDetail> GetTypeDict();
    void InvalidateCache();
    bool ExistType(Guid id);
    bool ExistType(string userTypeName);
}
