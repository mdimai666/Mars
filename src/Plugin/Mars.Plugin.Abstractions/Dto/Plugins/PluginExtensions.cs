using Mars.Server.Abstractions.Extensions;
using Mars.Plugin.Contracts.Plugins;

namespace Mars.Plugin.Abstractions.Dto.Plugins;

public static class PluginExtensions
{
    public static ListPluginQuery ToQuery(this ListPluginQueryRequest request)
        => new()
        {
            Skip = request.Skip,
            Take = request.Take,
            Search = request.Search,
            Sort = request.Sort,
        };

    public static ListPluginQuery ToQuery(this TablePluginQueryRequest request)
        => new()
        {
            Skip = request.ConvertPageAndPageSizeToSkip(),
            Take = request.PageSize,
            Search = request.Search,
            Sort = request.Sort,
        };

}
