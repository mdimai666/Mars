using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Extensions;
using Mars.Shared.Contracts.PostCategoryTypes;

namespace Mars.Host.Shared.Dto.PostCategoryTypes;

public static class PostCategoryTypeRequestExtensions
{
    public static CreatePostCategoryTypeQuery ToQuery(this CreatePostCategoryTypeRequest request)
        => new()
        {
            Title = request.Title,
            TypeName = request.TypeName,
            Id = request.Id,
            Tags = request.Tags,
            MetaFields = request.MetaFields.ToDto()
        };

    public static UpdatePostCategoryTypeQuery ToQuery(this UpdatePostCategoryTypeRequest request)
        => new()
        {
            Title = request.Title,
            TypeName = request.TypeName,
            Id = request.Id,
            Tags = request.Tags,
            MetaFields = request.MetaFields.ToDto()
        };

    public static ListPostCategoryTypeQuery ToQuery(this ListPostCategoryTypeQueryRequest request)
        => new()
        {
            Skip = request.Skip,
            Take = request.Take,
            Search = request.Search,
            Sort = request.Sort,
        };

    public static ListPostCategoryTypeQuery ToQuery(this TablePostCategoryTypeQueryRequest request)
        => new()
        {
            Skip = request.ConvertPageAndPageSizeToSkip(),
            Take = request.PageSize,
            Search = request.Search,
            Sort = request.Sort,
        };

}
