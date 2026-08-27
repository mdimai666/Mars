using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace AppFront.Shared.Components;

/// <summary>
/// Component for output [Display(Name, Description)] or Property Name
/// <example>
/// <code>
/// &lt;FormItem2 For=@(()=>x.Property)>
///  &lt;Input Value />
/// &lt;/FormItem2>
/// <label>Display name</label>
/// <para />
/// //output
/// DisplayName
/// [Value______]
/// @validationMessage
/// </code>
/// </example>
/// </summary>
/// <typeparam name="TValue"></typeparam>
public partial class FormItem2<TValue> : ComponentBase
{
    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [CascadingParameter] EditContext CurrentEditContext { get; set; } = default!;

    [Parameter] public Expression<Func<TValue>>? For { get; set; }

    [Parameter] public string Class { get; set; } = "";
    [Parameter] public string? Label { get; set; }

}
