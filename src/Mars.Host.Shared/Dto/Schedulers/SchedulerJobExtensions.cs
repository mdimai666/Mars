using Mars.Host.Shared.Extensions;
using Mars.Shared.Contracts.Schedulers;

namespace Mars.Host.Shared.Dto.Schedulers;

public static class SchedulerRequestExtensions
{
    public static ListSchedulerJobQuery ToQuery(this ListSchedulerJobQueryRequest request)
        => new()
        {
            Skip = request.Skip,
            Take = request.Take,
            Search = request.Search,
            Sort = request.Sort,
        };
    
    public static ListSchedulerJobQuery ToQuery(this TableSchedulerJobQueryRequest request)
        => new()
        {
            Skip = request.ConvertPageAndPageSizeToSkip(),
            Take = request.PageSize,
            Search = request.Search,
            Sort = request.Sort,
        };

}
