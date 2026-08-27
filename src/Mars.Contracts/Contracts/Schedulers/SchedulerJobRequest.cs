using Mars.Contracts.Common;

namespace Mars.Contracts.Schedulers;

public record ListSchedulerJobQueryRequest : BasicListQueryRequest
{
}

public record TableSchedulerJobQueryRequest : BasicTableQueryRequest
{
}
