using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Mars.Shared.Common;

public record BasicListQueryRequest : IBasicListRequest
{
    [DefaultValue(0)]
    public int Skip { get; init; }

    [Range(1, int.MaxValue)]
    [DefaultValue(BasicListQuery.DefaultPageSize)]
    public int Take { get; init; } = BasicListQuery.DefaultPageSize;

    [DefaultValue(null)]
    public virtual string? Search { get; init; }

    [DefaultValue(null)]
    public virtual string? Sort { get; init; }
}
