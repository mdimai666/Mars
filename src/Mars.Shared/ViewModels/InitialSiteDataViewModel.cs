using Mars.Shared.Contracts.NavMenus;
using Mars.Shared.Contracts.Options;
using Mars.Shared.Contracts.PostTypes;
using Mars.Shared.Contracts.XActions;
using Mars.Shared.Options;

namespace Mars.Shared.ViewModels;

public class InitialSiteDataViewModel
{
    public required SysOptions SysOptions { get; init; }
    public required UserPrimaryInfo? UserPrimaryInfo { get; init; }
    public required IReadOnlyCollection<PostTypeAdminPanelItemResponse> PostTypes { get; init; }
    public required IReadOnlyCollection<NavMenuDetailResponse> NavMenus { get; init; }

    public required IReadOnlyCollection<OptionSummaryResponse> Options { get; init; }

    public required IReadOnlyDictionary<string, XActionCommand> XActions { get; init; }
}
