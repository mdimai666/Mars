using Mars.Shared.Common;

namespace Mars.Shared.Contracts.Schedulers;

public record ListSchedulerJobQueryRequest : BasicListQueryRequest
{
}

public record TableSchedulerJobQueryRequest : BasicTableQueryRequest
{
}
