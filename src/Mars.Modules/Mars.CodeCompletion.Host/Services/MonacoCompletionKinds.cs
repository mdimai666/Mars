using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Tags;

namespace Mars.CodeCompletion.Host.Services;

internal static class MonacoCompletionKinds
{
    // имена Monaco languages.CompletionItemKind; на фронте парсятся в enum BlazorMonaco
    private static readonly Dictionary<string, string> RoslynTagToMonacoKind = new()
    {
        { WellKnownTags.Public, "Keyword" },
        { WellKnownTags.Protected, "Keyword" },
        { WellKnownTags.Private, "Keyword" },
        { WellKnownTags.Internal, "Keyword" },
        { WellKnownTags.File, "File" },
        { WellKnownTags.Project, "File" },
        { WellKnownTags.Folder, "Folder" },
        { WellKnownTags.Assembly, "File" },
        { WellKnownTags.Class, "Class" },
        { WellKnownTags.Constant, "Constant" },
        { WellKnownTags.Delegate, "Function" },
        { WellKnownTags.Enum, "Enum" },
        { WellKnownTags.EnumMember, "EnumMember" },
        { WellKnownTags.Event, "Event" },
        { WellKnownTags.ExtensionMethod, "Method" },
        { WellKnownTags.Field, "Field" },
        { WellKnownTags.Interface, "Interface" },
        { WellKnownTags.Intrinsic, "Text" },
        { WellKnownTags.Keyword, "Keyword" },
        { WellKnownTags.Label, "Text" },
        { WellKnownTags.Local, "Variable" },
        { WellKnownTags.Namespace, "Module" },
        { WellKnownTags.Method, "Method" },
        { WellKnownTags.Module, "Module" },
        { WellKnownTags.Operator, "Operator" },
        { WellKnownTags.Parameter, "Value" },
        { WellKnownTags.Property, "Property" },
        { WellKnownTags.RangeVariable, "Variable" },
        { WellKnownTags.Reference, "Reference" },
        { WellKnownTags.Structure, "Struct" },
        { WellKnownTags.TypeParameter, "TypeParameter" },
        { WellKnownTags.Snippet, "Snippet" },
        { WellKnownTags.Error, "Issue" },
        { WellKnownTags.Warning, "Issue" },
    };

    public static string FromRoslynTags(ImmutableArray<string> tags)
    {
        foreach (var tag in tags)
        {
            if (RoslynTagToMonacoKind.TryGetValue(tag, out var kind))
                return kind;
        }

        return "Text";
    }
}
