using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Extensions;
using Mars.Shared.Contracts.UserTypes;

namespace Mars.Host.Shared.Dto.UserTypes;

public static class UserTypeRequestExtensions
{
    public static CreateUserTypeQuery ToQuery(this CreateUserTypeRequest request)
        => new()
        {
            Title = request.Title,
            TypeName = request.TypeName,
            Id = request.Id,
            Tags = request.Tags,
            MetaFields = request.MetaFields.ToDto()
        };

    public static UpdateUserTypeQuery ToQuery(this UpdateUserTypeRequest request)
        => new()
        {
            Title = request.Title,
            TypeName = request.TypeName,
            Id = request.Id,
            Tags = request.Tags,
            MetaFields = request.MetaFields.ToDto()
        };

    public static ListUserTypeQuery ToQuery(this ListUserTypeQueryRequest request)
        => new()
        {
            Skip = request.Skip,
            Take = request.Take,
            Search = request.Search,
            Sort = request.Sort,
        };

    public static ListUserTypeQuery ToQuery(this TableUserTypeQueryRequest request)
        => new()
        {
            Skip = request.ConvertPageAndPageSizeToSkip(),
            Take = request.PageSize,
            Search = request.Search,
            Sort = request.Sort,
        };

}
