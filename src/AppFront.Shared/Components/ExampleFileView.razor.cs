using Microsoft.AspNetCore.Components;

namespace AppFront.Shared.Components;

public partial class ExampleFileView
{
    [Parameter]
    public string DocName { get; set; } = default!;
    [Parameter]
    public string Ext { get; set; } = "pdf";
    public string URL => Q.ServerUrlJoin($"/examples/{DocName}.{Ext}");
}
