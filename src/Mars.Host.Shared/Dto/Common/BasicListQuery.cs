namespace Mars.Host.Shared.Dto.Common;

//public record BasicListQuery : IBasicListQuery
//{
//    public const int DefaultPageSize = 20;
//    public const int MaxPageSize = 1000;

//    public int Page { get; }
//    public int PageSize { get; }
//    public bool IncludeTotalCount { get; init; }

//    public BasicListQuery(int page = 1, int? pageSize = DefaultPageSize)
//    {
//        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1, nameof(page));

//        if (pageSize is not null)
//        {
//            ArgumentOutOfRangeException.ThrowIfLessThan(pageSize.Value, 1, nameof(pageSize));
//            ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize.Value, MaxPageSize, nameof(pageSize));
//        }
//        Page = page;
//        PageSize = pageSize ?? MaxPageSize;
//    }

//    //public BasicListQuery(OptionReading option) : this()
//    //{

//    //}

//    public int Skip => (Page - 1) * PageSize;
//    public int Take => PageSize;
//}

//public interface IBasicListQuery
//{
//    public int Page { get; }
//    public int PageSize { get; }
//}
