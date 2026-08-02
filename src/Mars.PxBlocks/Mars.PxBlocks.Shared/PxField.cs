namespace Mars.PxBlocks.Shared;

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
