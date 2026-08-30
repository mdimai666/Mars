using Mars.Contracts.Common;

namespace Mars.Scheduler.Contracts.Schedulers;

public record ListSchedulerJobQueryRequest : BasicListQueryRequest
{
}

public record TableSchedulerJobQueryRequest : BasicTableQueryRequest
{
}
