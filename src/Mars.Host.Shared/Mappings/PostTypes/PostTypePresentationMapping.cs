using Mars.Host.Shared.Dto.PostTypes;
using Mars.Shared.Contracts.PostTypes;

namespace Mars.Host.Shared.Mappings.PostTypes;

public static class PostTypePresentationMapping
{
    public static PostTypePresentationResponse ToResponse(this PostTypePresentation entity)
        => new()
        {
            ListViewTemplate = entity.ListViewTemplate,
        };
}
