using Mars.Shared.Common;

namespace Mars.Host.Shared.Extensions;

public static class IBasicTableRequestExtensions
{
    public static int ConvertPageAndPageSizeToSkip(this IBasicTableRequest request) => (request.Page - 1) * request.PageSize;
}
