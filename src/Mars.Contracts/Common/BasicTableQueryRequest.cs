using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Mars.Shared.Common;

public record BasicTableQueryRequest : IBasicTableRequest
{
    [DefaultValue(1)]
    [Range(1, int.MaxValue)]
    public virtual int Page { get; init; } = 1;

    [DefaultValue(BasicListQuery.DefaultPageSize)]
    [Range(1, BasicListQuery.MaxPageSize)]
    public virtual int PageSize { get; init; } = BasicListQuery.DefaultPageSize;

    [DefaultValue(null)]
    public virtual string? Search { get; init; }

    [DefaultValue(null)]
    public virtual string? Sort { get; init; }
    
}
