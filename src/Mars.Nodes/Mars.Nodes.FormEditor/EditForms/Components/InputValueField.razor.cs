using Mars.Nodes.Core.Fields;
using Microsoft.AspNetCore.Components;

namespace Mars.Nodes.FormEditor.EditForms.Components;

public partial class InputValueField<T>
{
    private string _stringValue = "";

    InputValue<T> _value = new();

    /* THIS FILE NOT WORK. ON RUN FREEZE RECURSE UPDATE */

    [Parameter]
    public InputValue<T>? Value
    {
        get => _value;
        set
        {
            if (value == _value) return;
            _value = value!;
            ValueChanged.InvokeAsync(_value);
        }
    }
    [Parameter] public EventCallback<InputValue<T>> ValueChanged { get; set; }

    [Parameter] public string? Placeholder { get; set; }

    protected override void OnParametersSet()
    {
        _value = Value?.ToString() ?? "";
    }

    private void OnStringValueChanged(string text)
    {
        if (_stringValue == text) return;
        _stringValue = text;
        Value = _stringValue;

        //_value = text;
        //var parsed = InputValue<T>.Parse(text);
        //Value = parsed;
        //await ValueChanged.InvokeAsync(parsed);
    }

    private string StatusText
    {
        get
        {
            if (Value is null)
                return "";

            if (Value.IsJsonLiteral)
                return "JSON literal";

            if (Value.IsEvalExpression)
                return Value.IsSingleName
                    ? "Expression (variable)"
                    : "Expression";

            return "Literal";
        }
    }

    private RenderFragment RightButtonContent => builder =>
    {
        if (Value?.IsJsonLiteral == true)
        {
            builder.AddContent(0, "JSON");
            return;
        }

        if (Value?.IsEvalExpression == true)
        {
            builder.AddContent(1, "ƒx");
            return;
        }

        builder.AddContent(2, GetLiteralIcon());
    };

    private string GetLiteralIcon()
    {
        var t = typeof(T);

        if (t == typeof(string)) return "ABC";
        if (t == typeof(int) || t == typeof(double) || t == typeof(decimal)) return "123";
        if (t == typeof(bool)) return "⊨";
        if (t.IsEnum) return "≡";

        return "●";
    }
}
