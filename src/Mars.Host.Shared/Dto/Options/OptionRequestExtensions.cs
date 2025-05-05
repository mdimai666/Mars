using Mars.Host.Shared.Extensions;
using Mars.Shared.Contracts.Options;

namespace Mars.Host.Shared.Dto.Options;

public static class OptionRequestExtensions
{
    //public static CreateOptionQuery ToQuery(this CreateOptionRequest request)
    //    => new()
    //    {
    //        Title = request.Title,
    //        TypeName = request.TypeName,
    //        Id = request.Id,
    //        Tags = request.Tags ?? []
    //    };
    //public static UpdateOptionQuery ToQuery(this UpdateOptionRequest request)
    //    => new()
    //    {
    //        Title = request.Title,
    //        TypeName = request.TypeName,
    //        Id = request.Id,
    //        Tags = request.Tags ?? []
    //    };

    public static ListOptionQuery ToQuery(this ListOptionQueryRequest request)
        => new()
        {
            Skip = request.Skip,
            Take = request.Take,
            Search = request.Search,
            Sort = request.Sort,
        };

    public static ListOptionQuery ToQuery(this TableOptionQueryRequest request)
        => new()
        {
            Skip = request.ConvertPageAndPageSizeToSkip(),
            Take = request.PageSize,
            Search = request.Search,
            Sort = request.Sort,
        };
}
