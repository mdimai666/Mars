using System.Text.Json.Serialization;
using Mars.Shared.Common;
using Mars.Shared.Contracts.NavMenus;
using Mars.Shared.Contracts.Options;
using Mars.Shared.Contracts.PostTypes;
using Mars.Shared.Contracts.Renders;
using Mars.Shared.Contracts.XActions;
using Mars.Shared.Options;

namespace Mars.Shared.ViewModels;

public class InitialSiteDataViewModel
{
    public required SysOptions SysOptions { get; init; }
    public required UserPrimaryInfo? UserPrimaryInfo { get; init; }
    public required IReadOnlyCollection<PostTypeSummaryResponse> PostTypes { get; init; }
    public required IReadOnlyCollection<NavMenuDetailResponse> NavMenus { get; init; }

    [JsonIgnore]
    public RenderActionResult<PostRenderResponse>? IndexPage => LocalPages.FirstOrDefault(s => s.Data?.PostSlug == "index");
    public required IReadOnlyCollection<RenderActionResult<PostRenderResponse>> LocalPages { get; init; }

    public required IReadOnlyCollection<OptionSummaryResponse> Options { get; init; }

    public required IReadOnlyDictionary<string, XActionCommand> XActions { get; init; }
}
