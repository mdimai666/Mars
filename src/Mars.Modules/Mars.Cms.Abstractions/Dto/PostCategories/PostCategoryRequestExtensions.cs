using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.PostCategoryTypes;
using Mars.Host.Shared.Extensions;
using Mars.Shared.Contracts.PostCategories;

namespace Mars.Host.Shared.Dto.PostCategories;

public static class PostCategoryRequestExtensions
{
    public static CreatePostCategoryQuery ToQuery(this CreatePostCategoryRequest request, Guid postCategoryTypeId, Guid postTypeId, IDictionary<Guid, MetaFieldDto> metaFields)
        => new()
        {
            Id = request.Id,
            Title = request.Title,
            Slug = request.Slug,
            Tags = request.Tags,

            ParentId = request.ParentId,
            PostTypeId = postTypeId,
            PostCategoryTypeId = postCategoryTypeId,
            Disabled = request.Disabled,

            MetaValues = request.MetaValues.ToQuery(metaFields),
        };

    public static UpdatePostCategoryQuery ToQuery(this UpdatePostCategoryRequest request, Guid postCategoryTypeId, Guid postTypeId, IDictionary<Guid, MetaFieldDto> metaFields)
        => new()
        {
            Id = request.Id,
            Title = request.Title,
            Slug = request.Slug,
            Tags = request.Tags,

            ParentId = request.ParentId,
            PostTypeId = postTypeId,
            PostCategoryTypeId = postCategoryTypeId,
            Disabled = request.Disabled,

            MetaValues = request.MetaValues.ToQuery(metaFields),
        };

    public static ListPostCategoryQuery ToQuery(this ListPostCategoryQueryRequest request, string? postTypeName, string? categoryType)
        => new()
        {
            Skip = request.Skip,
            Take = request.Take,
            Search = request.Search,
            Sort = request.Sort,

            Type = categoryType,
            PostTypeName = postTypeName,
        };

    public static ListPostCategoryQuery ToQuery(this TablePostCategoryQueryRequest request, string? postTypeName, string? categoryType)
        => new()
        {
            Skip = request.ConvertPageAndPageSizeToSkip(),
            Take = request.PageSize,
            Search = request.Search,
            Sort = request.Sort,

            Type = categoryType,
            PostTypeName = postTypeName,
        };

}
