using System.ComponentModel.DataAnnotations;

namespace Mars.Shared.Common;

public record BasicListQuery : IBasicListQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 1000;

    public int Skip { get; init; }

    [Range(1, MaxPageSize)]
    public int Take { get; init; } = DefaultPageSize;
    public string? Sort { get; init; }


    public string? Search { get; init; }
    public bool IncludeTotalCount { get; init; } = true;


    public static BasicListQuery FromPage(int page, int? pageSize = DefaultPageSize, string? sort = null, string? search = null)
        => new BasicListQuery
        {
            Skip = (page - 1) * (pageSize ?? DefaultPageSize),
            Take = pageSize ?? DefaultPageSize,
            Sort = sort,
            Search = search,
        };

    public int ConvertSkipTakeToPage() => ConvertSkipTakeToPage(Skip, Take);

    public static int ConvertSkipTakeToPage(int skip, int take)
    {
        if (skip < 0 || take <= 0)
        {
            throw new ArgumentException("Invalid arguments: skip must be non-negative, take must be positive.");
        }

        int page = (skip / take) + 1;
        //int pageSize = take;

        //return (page, pageSize);
        return page;
    }
}
