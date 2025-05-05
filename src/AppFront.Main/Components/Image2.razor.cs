using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace AppFront.Shared.Components;

public partial class Image2 : FluentComponentBase
{
    [Parameter] public string? Src { get; set; }
    [Parameter] public string? PreviewSrc { get; set; }
    [Parameter] public string? Alt { get; set; }
    [Parameter] public string? Title { get; set; }
}
