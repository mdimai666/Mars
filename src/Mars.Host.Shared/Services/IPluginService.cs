using Mars.Host.Shared.Dto.Plugins;
using Mars.Shared.Common;

namespace Mars.Host.Shared.Services;

public interface IPluginService
{
    ListDataResult<PluginInfoDto> List(ListPluginQuery query);
    PagingResult<PluginInfoDto> ListTable(ListPluginQuery query);
}
