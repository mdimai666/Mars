namespace Mars.Shared.Common;

public interface IBasicListRequest
{
    int Skip { get; init; }
    int Take { get; }
    string? Search { get; }
    string? Sort { get; }
}

public interface IBasicTableRequest
{
    int Page { get; init; }
    int PageSize { get; init; }
    string? Search { get; }
    string? Sort { get; }
}
