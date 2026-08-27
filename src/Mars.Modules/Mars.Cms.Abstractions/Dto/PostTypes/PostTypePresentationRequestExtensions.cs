using Mars.Contracts.PostTypes;

namespace Mars.Cms.Abstractions.Dto.PostTypes;

public static class PostTypePresentationRequestExtensions
{
    public static UpdatePostTypePresentationQuery ToQuery(this UpdatePostTypePresentationRequest request)
        => new()
        {
            Id = request.Id,
            ListViewTemplate = request.ListViewTemplate,
            Grid = request.Grid,
        };
}
