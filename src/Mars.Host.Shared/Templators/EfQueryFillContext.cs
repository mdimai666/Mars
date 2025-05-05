using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Dto.Users;

namespace Mars.Host.Shared.Templators;

public class EfQueryFillContext
{
    public UserDetail? User { get; init; }
    public Dictionary<Guid, MetaRelationObjectDict> FillDict { get; init; }
    public PostTypeDetail PostType { get; init; }

    //public EfQueryFillContext()
    //{

    //}

    public EfQueryFillContext(Dictionary<Guid, MetaRelationObjectDict> fillDict, PostTypeDetail postType, UserDetail? user)
    {
        FillDict = fillDict;
        PostType = postType;
        User = user;
    }
}
