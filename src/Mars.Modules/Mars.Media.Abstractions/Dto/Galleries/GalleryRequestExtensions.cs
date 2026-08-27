using Mars.Host.Shared.Extensions;
using Mars.Shared.Contracts.Galleries;

namespace Mars.Host.Shared.Dto.Galleries;

public static class GalleryRequestExtensions
{
    public static CreateGalleryQuery ToQuery(this CreateGalleryRequest request, Guid userId)
        => new()
        {
            //Id = request.Id,
            //Title = request.Title,
            //Type = request.Type,
            //Slug = request.Slug,
            //Tags = request.Tags,
            //Content = request.Content,
            //Status = request.Status,
            //UserId = userId
        };
    public static UpdateGalleryQuery ToQuery(this UpdateGalleryRequest request)
        => new()
        {
            //Id = request.Id,
            //Title = request.Title,
            //Type = request.Type,
            //Slug = request.Slug,
            //Tags = request.Tags
        };

    public static ListGalleryQuery ToQuery(this ListGalleryQueryRequest request, string? postTypeName)
        => new()
        {
            Skip = request.Skip,
            Take = request.Take,
            Search = request.Search,
            Sort = request.Sort,

            Type = postTypeName,
        };

    public static ListGalleryQuery ToQuery(this TableGalleryQueryRequest request, string? postTypeName)
        => new()
        {
            Skip = request.ConvertPageAndPageSizeToSkip(),
            Take = request.PageSize,
            Search = request.Search,
            Sort = request.Sort,

            Type = postTypeName,
        };
}
