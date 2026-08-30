using Mars.Cms.Contracts.NavMenus;
using Mars.Cms.Contracts.PostTypes;
using Mars.Identity.Contracts.ViewModels;
using Mars.Options.Contracts.Dto.Options;
using Mars.Server.Contracts.Options;
using Mars.XActions.Contracts;

namespace Mars.Server.Contracts.ViewModels;

public class InitialSiteDataViewModel
{
    public required SiteSettings SiteSettings { get; init; }

    /// <summary>
    /// Рабочая директория серверного процесса (для дев-инструментов админки вида «открыть исходник в IDE»).
    /// </summary>
    public string ServerWorkDirectory { get; init; } = "";
    public required UserPrimaryInfo? UserPrimaryInfo { get; init; }
    public required IReadOnlyCollection<PostTypeAdminPanelItemResponse> PostTypes { get; init; }
    public required IReadOnlyCollection<NavMenuDetailResponse> NavMenus { get; init; }

    public required IReadOnlyCollection<OptionSummaryResponse> Options { get; init; }

    public required IReadOnlyDictionary<string, XActionCommand> XActions { get; init; }
}
