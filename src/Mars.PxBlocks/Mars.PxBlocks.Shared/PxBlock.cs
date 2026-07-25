using System.Text.Json.Serialization;

namespace Mars.PxBlocks.Shared;

public abstract class PxBlock
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public virtual string TypeId => GetType().FullName!;

    [JsonIgnore]
    public virtual string Label => GetType().Name.EndsWith("Block")
        ? GetType().Name[..^5]
        : GetType().Name;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float X { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float Y { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float Z { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Container { get; set; } = "";

    [JsonIgnore]
    public virtual string DisplayName => Label;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public virtual string Color { get; set; } = "#A8A8A8";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public virtual string Icon { get; set; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Disabled { get; set; }

    public List<PxField> Fields { get; set; } = [];

    public List<PxInput> Inputs { get; set; } = [];

    [JsonIgnore]
    public bool IsDragging { get; set; }
}

// --- Statement block (stacks vertically) ---

public class PxBlockCommand : PxBlock
{
    public string? PreviousBlockId { get; set; }
    public string? NextBlockId { get; set; }

    public PxBlockCommand()
    {
        Color = "#5C81A6";
    }
}

// --- Event / Hat block (top-level, no previous connection, rounded top) ---

public class PxBlockHat : PxBlock
{
    public string? NextBlockId { get; set; }

    public PxBlockHat()
    {
        Color = "#A5745B";
    }
}

// --- Value / Reporter block (returns a value, has output connector) ---

public class PxBlockValue : PxBlock
{
    public string OutputType { get; set; } = "string";

    public PxBlockValue()
    {
        Color = "#8C5BA5";
    }
}

// --- Input slot on a block ---

public class PxInput
{
    public string Name { get; set; } = "";
    public PxInputType Type { get; set; } = PxInputType.Value;
    public string? ConnectedBlockId { get; set; }

    public List<PxField> Fields { get; set; } = [];
}

public enum PxInputType
{
    Value,
    Statement
}

// --- Fields (inline UI elements on the block) ---

public abstract class PxField
{
    public string Name { get; set; } = "";
}

public class PxLabelField : PxField
{
    public string Text { get; set; } = "";
}

public class PxTextField : PxField
{
    public string Value { get; set; } = "";
    public string? Placeholder { get; set; }
}

public class PxNumberField : PxField
{
    public double Value { get; set; }
    public double? Min { get; set; }
    public double? Max { get; set; }
}

public class PxDropdownField : PxField
{
    public List<string> Options { get; set; } = [];
    public int SelectedIndex { get; set; }
    public string SelectedValue => Options.Count > 0 && SelectedIndex >= 0 && SelectedIndex < Options.Count
        ? Options[SelectedIndex]
        : "";
}

public class PxCheckboxField : PxField
{
    public bool Checked { get; set; }
}

public class PxVariableField : PxField
{
    public string VariableName { get; set; } = "";
}
