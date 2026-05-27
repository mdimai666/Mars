namespace Mars.Nodes.FormEditor.EditForms.Components;

public class OperationInputEditModel
{
    private Guid Id;
    public string HtmlElementId { get; init; }
    public string Name { get; init; }
    public string Value { get; set; }
    public TypeCode Type { get; init; }
    public string? Placeholder { get; init; }
    public string? Description { get; init; }
    public bool IsRequired { get; init; }

    public bool ValueBoolSetter { get => bool.TryParse(Value, out var b) ? b : false; set => Value = value.ToString(); }

    public int ValueIntSetter { get => int.TryParse(Value, out var i) ? i : 0; set => Value = value.ToString(); }

    public OperationInputEditModel(string name, string value, TypeCode type, string? placeholder = null, string? description = null, bool isRequired = false)
    {
        Id = Guid.NewGuid();
        HtmlElementId = $"operation-input-{Id}";

        Name = name;
        Value = value;
        Type = type;
        Placeholder = placeholder;
        Description = description;
        IsRequired = isRequired;
    }
}
