using System.Text.RegularExpressions;
using Mars.Nodes.Core.Nodes.Common;

namespace Mars.Nodes.Core;

/// <summary>
/// Syntax of property paths ("msg.Payload.User.Name", relative "Payload.items[0].x").
/// Single source for the root list: expression regexes and editor inputs derive from it.
/// </summary>
public static class ValuePath
{
    public const string MsgRoot = "msg";

    public static readonly string[] Roots = [MsgRoot, "GlobalContext", "FlowContext", nameof(VarNode)];

    static readonly Regex PathRegex = new(
        @"^[A-Za-z_][A-Za-z0-9_]*(\[\d*\])?(\.[A-Za-z_][A-Za-z0-9_]*(\[\d*\])?)*$", RegexOptions.Compiled);

    public static bool IsSyntaxValid(string? path)
        => !string.IsNullOrWhiteSpace(path) && PathRegex.IsMatch(path.Trim());

    /// <summary>Known root of the path ("msg", "GlobalContext", …) or null when the first segment is not a root.</summary>
    public static string? GetRoot(string path)
    {
        var dot = path.IndexOf('.');
        var root = dot < 0 ? path : path[..dot];
        return Roots.FirstOrDefault(r => string.Equals(r, root, StringComparison.Ordinal));
    }

    /// <summary>Path without the root segment; empty for a bare root.</summary>
    public static string GetRest(string path)
    {
        var dot = path.IndexOf('.');
        return dot < 0 ? "" : path[(dot + 1)..];
    }

    public static string[] GetSegments(string path) => path.Split('.');
}
