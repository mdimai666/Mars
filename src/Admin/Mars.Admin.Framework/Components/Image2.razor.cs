using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Framework.Components;

public partial class Image2 : FluentComponentBase
{
    public Image2(LibraryConfiguration configuration) : base(configuration)
    {
    }
    [Parameter] public string? Src { get; set; }
    [Parameter] public string? PreviewSrc { get; set; }
    [Parameter] public string? Alt { get; set; }
    [Parameter] public string? Title { get; set; }
}
