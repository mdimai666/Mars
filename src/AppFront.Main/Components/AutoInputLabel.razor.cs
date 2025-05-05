using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using Mars.Shared.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Localization;

namespace AppFront.Shared.Components;

public partial class AutoInputLabel<TValue> : ComponentBase, IDisposable
{
    protected EditContext? _previousEditContext;
    protected Expression<Func<TValue>>? _previousFieldAccessor;
    protected readonly EventHandler<ValidationStateChangedEventArgs>? _validationStateChangedHandler;
    protected FieldIdentifier _fieldIdentifier;

    [Inject] IStringLocalizer<AppRes> L { get; set; } = default!;

    /// <summary>
    /// Gets or sets a collection of additional attributes that will be applied to the created <c>div</c> element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [CascadingParameter]
    EditContext CurrentEditContext { get; set; } = default!;

    [CascadingParameter]
    EditForm form { get; set; } = default!;

    /// <summary>
    /// Specifies the field for which validation messages should be displayed.
    /// </summary>
    [Parameter]
    public Expression<Func<TValue>>? For { get; set; }

    protected string _displayName = "";
    protected string? _description;
    public string? Description => _description;

    protected bool _required;

    [Parameter] public RenderFragment ChildContent { get; set; } = default!;
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Constructs an instance of <see cref="AutoInputLabel{TValue}"/>.
    /// </summary>
    public AutoInputLabel()
    {
        _validationStateChangedHandler = (sender, eventArgs) => StateHasChanged();
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (CurrentEditContext == null)
        {
            throw new InvalidOperationException($"{GetType()} requires a cascading parameter " +
                $"of type {nameof(EditContext)}. For example, you can use {GetType()} inside " +
                $"an {nameof(EditForm)}.");
        }

        if (For == null) // Not possible except if you manually specify T
        {
            throw new InvalidOperationException($"{GetType()} requires a value for the " +
                $"{nameof(For)} parameter.");
        }
        else if (For != _previousFieldAccessor)
        {
            _fieldIdentifier = FieldIdentifier.Create(For);
            _previousFieldAccessor = For;
        }

        if (CurrentEditContext != _previousEditContext)
        {
            DetachValidationStateChangedListener();
            CurrentEditContext.OnValidationStateChanged += _validationStateChangedHandler;
            _previousEditContext = CurrentEditContext;
        }

        _displayName = Label ?? _fieldIdentifier.FieldName;
        var prop = _fieldIdentifier.Model.GetType().GetProperty(_fieldIdentifier.FieldName);

        DisplayAttribute? attr;

        if (prop is not null)
        {
            attr = prop.GetCustomAttribute<DisplayAttribute>();

            if (!string.IsNullOrEmpty(attr?.Description))
            {
                _description = attr?.Description;
            }
        }
        else
        {
            var field = _fieldIdentifier.Model.GetType().GetField(_fieldIdentifier.FieldName);
            attr = field?.GetCustomAttribute<DisplayAttribute>();
        }

        _required = prop?.GetCustomAttribute<RequiredAttribute>() != null ? true : false;

        if (Label is null && attr?.ResourceType is not null)
        {
            _displayName = L[attr?.Name ?? _displayName];
        }
        else
        {
            _displayName = Label ?? attr?.Name ?? _displayName;
        }

        // Добавляем вызов StateHasChanged(), чтобы перерисовать компонент
        StateHasChanged();
    }

    /// <inheritdoc />
    //protected override void BuildRenderTree(RenderTreeBuilder builder)
    //{
    //    builder.OpenElement(0, "label");
    //    builder.AddAttribute(1, "class", "fluent-input-label");
    //    builder.AddMultipleAttributes(2, AdditionalAttributes);
    //    builder.AddContent(3, _displayName);
    //    builder.CloseElement();

    //    builder.AddContent(4, ChildContent);

    //    if (!string.IsNullOrWhiteSpace(_description))
    //    {
    //        builder.OpenElement(5, "div");
    //        builder.AddAttribute(6, "class", "d-fluent-input-description");
    //        builder.AddContent(7, _description);
    //        builder.CloseElement();
    //    }
    //}

    /// <summary>
    /// Called to dispose this instance.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> if called within <see cref="IDisposable.Dispose"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
    }

    void IDisposable.Dispose()
    {
        DetachValidationStateChangedListener();
        Dispose(disposing: true);
    }

    private void DetachValidationStateChangedListener()
    {
        if (_previousEditContext != null)
        {
            _previousEditContext.OnValidationStateChanged -= _validationStateChangedHandler;
        }
    }
}
