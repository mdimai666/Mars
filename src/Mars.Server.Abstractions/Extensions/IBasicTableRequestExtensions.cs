using Mars.Contracts.Common;

namespace Mars.Server.Abstractions.Extensions;

public static class IBasicTableRequestExtensions
{
    public static int ConvertPageAndPageSizeToSkip(this IBasicTableRequest request) => (request.Page - 1) * request.PageSize;
}
