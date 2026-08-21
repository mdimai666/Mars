using Microsoft.AspNetCore.Components;

namespace AppFront.Shared.Components.MetaFieldViews;

public partial class FormMetaValue
{
    [CascadingParameter] List<MetaValueEditModel> MetaValues { get; set; } = default!;

    [CascadingParameter] List<MetaFieldEditModel> MetaFields { get; set; } = default!;

    [Parameter] public bool Vertical { get; set; }
    [Parameter] public bool Client { get; set; }
}
