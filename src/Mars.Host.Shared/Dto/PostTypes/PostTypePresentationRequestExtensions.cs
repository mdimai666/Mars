using Mars.Shared.Contracts.PostTypes;

namespace Mars.Host.Shared.Dto.PostTypes;

public static class PostTypePresentationRequestExtensions
{
    public static UpdatePostTypePresentationQuery ToQuery(this UpdatePostTypePresentationRequest request)
        => new()
        {
            Id = request.Id,
            ListViewTemplate = request.ListViewTemplate,
        };
}
