using Mars.Contracts.NavMenus;
using Mars.Contracts.Options;
using Mars.Contracts.PostTypes;
using Mars.Contracts.XActions;
using Mars.Contracts.ViewModels;

namespace Mars.Server.Contracts.ViewModels;

public class InitialSiteDataViewModel
{
    public required SysOptions SysOptions { get; init; }
    public required UserPrimaryInfo? UserPrimaryInfo { get; init; }
    public required IReadOnlyCollection<PostTypeAdminPanelItemResponse> PostTypes { get; init; }
    public required IReadOnlyCollection<NavMenuDetailResponse> NavMenus { get; init; }

    public required IReadOnlyCollection<OptionSummaryResponse> Options { get; init; }

    public required IReadOnlyDictionary<string, XActionCommand> XActions { get; init; }
}
