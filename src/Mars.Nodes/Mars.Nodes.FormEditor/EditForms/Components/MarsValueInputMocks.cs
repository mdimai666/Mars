namespace Mars.Nodes.FormEditor.EditForms.Components;

public static class MarsValueInputMocks
{
    public static IReadOnlyList<ValueOpGroup> Operations { get; } =
    [
        new("String", [".ToUpper()", ".ToLower()", ".Trim()", ".Length", ".Substring(0, 5)", ".Replace(\"a\", \"b\")"]),
        new("Math", [" + 1", " - 1", " * 2", " / 2", "Math.Round(", "Math.Abs("]),
        new("Convert", [".ToString()", "int.Parse(", "double.Parse(", "Convert.ToInt32("]),
        new("Date", [".AddDays(1)", ".Year", ".ToString(\"yyyy-MM-dd\")"]),
        new("Logic", [" == null", " != null", " ?? 0", " ? 1 : 0"]),
    ];
}

public record ValueOpGroup(string Title, IReadOnlyList<string> Items);
