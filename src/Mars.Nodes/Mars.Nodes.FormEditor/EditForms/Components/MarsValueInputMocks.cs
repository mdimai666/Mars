namespace Mars.Nodes.FormEditor.EditForms.Components;

public record ValueFieldInfo(string Path, string Type);

public static class MarsValueInputMocks
{
    public static IReadOnlyList<ValueFieldInfo> Fields { get; } =
    [
        new("msg.payload.age", "number"),
        new("msg.payload.name", "string"),
        new("msg.payload.items", "List<string>"),
        new("msg.user.email", "string"),
        new("msg.user.firstname", "string"),
        new("msg.user.age", "number"),
        new("msg.user.registered", "DateTime"),
        new("msg.topic", "string"),
        new("msg.timestamp", "DateTime"),
        new("flow.counter", "number"),
        new("flow.lastError", "string"),
        new("global.var1", "string"),
        new("VarNode.siteName", "string"),
    ];

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
