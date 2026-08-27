using Mars.Cms.Abstractions.Dto.PostTypes;
using Mars.Contracts.PostTypes;

namespace Mars.Cms.Abstractions.Mappings.PostTypes;

public static class PostTypePresentationMapping
{
    public static PostTypePresentationResponse ToResponse(this PostTypePresentation entity)
        => new()
        {
            ListViewTemplate = entity.ListViewTemplate,
            Grid = entity.Grid,
        };
}
