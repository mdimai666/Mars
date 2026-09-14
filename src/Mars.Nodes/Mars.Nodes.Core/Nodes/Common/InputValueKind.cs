namespace Mars.Nodes.Core.Nodes.Common;

public static class InputValueKind
{
    public const string Const = "const";
    public const string Expression = "expression";

    public static bool IsValid(string? kind) => kind is Const or Expression;
}
