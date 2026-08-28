using Mars.Data.Extensions;
using Mars.Scheduler.Abstractions.Dto.Schedulers;
using Mars.Contracts.Common;
using Mars.Scheduler.Contracts.Schedulers;

namespace Mars.Scheduler.Abstractions.Mappings.Schedulers;

public static class SchedulerMapping
{
    public static SchedulerJobResponse ToResponse(this SchedulerJobDto entity)
        => new()
        {
            Name = entity.Name,
            Group = entity.Group,
            NextExecutionTime = entity.NextExecutionTime,
            Triggers = entity.Triggers.ToResponse(),
        };

    public static SchedulerMaskResponse ToResponse(this SchedulerMaskDto entity)
        => new()
        {
            Description = entity.Description,
            IsCron = entity.IsCron,
            Mask = entity.Mask,
            TriggerState = entity.TriggerState,
        };

    public static IReadOnlyCollection<SchedulerJobResponse> ToResponse(this IReadOnlyCollection<SchedulerJobDto> list)
        => list.Select(ToResponse).ToList();

    public static IReadOnlyCollection<SchedulerMaskResponse> ToResponse(this IReadOnlyCollection<SchedulerMaskDto> list)
        => list.Select(ToResponse).ToList();

    public static ListDataResult<SchedulerJobResponse> ToResponse(this ListDataResult<SchedulerJobDto> list)
        => list.ToMap(ToResponse);

    public static PagingResult<SchedulerJobResponse> ToResponse(this PagingResult<SchedulerJobDto> list)
        => list.ToMap(ToResponse);
}
