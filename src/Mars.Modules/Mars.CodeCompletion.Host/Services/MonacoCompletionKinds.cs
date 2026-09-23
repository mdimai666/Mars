using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Tags;

namespace Mars.CodeCompletion.Host.Services;

internal static class MonacoCompletionKinds
{
    // Monaco languages.CompletionItemKind
    public const int Text = 1;
    public const int Method = 2;
    public const int Function = 3;
    public const int Constructor = 4;
    public const int Field = 5;
    public const int Variable = 6;
    public const int Class = 7;
    public const int Interface = 8;
    public const int Module = 9;
    public const int Property = 10;
    public const int Unit = 11;
    public const int Value = 12;
    public const int Enum = 13;
    public const int Keyword = 14;
    public const int Snippet = 15;
    public const int Color = 16;
    public const int File = 17;
    public const int Reference = 18;
    public const int Folder = 19;
    public const int EnumMember = 20;
    public const int Constant = 21;
    public const int Struct = 22;
    public const int Event = 23;
    public const int Operator = 24;
    public const int TypeParameter = 25;

    private static readonly Dictionary<string, int> RoslynTagToMonacoKind = new()
    {
        { WellKnownTags.Public, Keyword },
        { WellKnownTags.Protected, Keyword },
        { WellKnownTags.Private, Keyword },
        { WellKnownTags.Internal, Keyword },
        { WellKnownTags.File, File },
        { WellKnownTags.Project, File },
        { WellKnownTags.Folder, Folder },
        { WellKnownTags.Assembly, File },
        { WellKnownTags.Class, Class },
        { WellKnownTags.Constant, Constant },
        { WellKnownTags.Delegate, Function },
        { WellKnownTags.Enum, Enum },
        { WellKnownTags.EnumMember, EnumMember },
        { WellKnownTags.Event, Event },
        { WellKnownTags.ExtensionMethod, Method },
        { WellKnownTags.Field, Field },
        { WellKnownTags.Interface, Interface },
        { WellKnownTags.Intrinsic, Text },
        { WellKnownTags.Keyword, Keyword },
        { WellKnownTags.Label, Text },
        { WellKnownTags.Local, Variable },
        { WellKnownTags.Namespace, Module },
        { WellKnownTags.Method, Method },
        { WellKnownTags.Module, Module },
        { WellKnownTags.Operator, Operator },
        { WellKnownTags.Parameter, Value },
        { WellKnownTags.Property, Property },
        { WellKnownTags.RangeVariable, Variable },
        { WellKnownTags.Reference, Reference },
        { WellKnownTags.Structure, Struct },
        { WellKnownTags.TypeParameter, TypeParameter },
        { WellKnownTags.Snippet, Snippet },
        { WellKnownTags.Error, Text },
        { WellKnownTags.Warning, Text },
    };

    public static int FromRoslynTags(ImmutableArray<string> tags)
    {
        foreach (var tag in tags)
        {
            if (RoslynTagToMonacoKind.TryGetValue(tag, out var kind))
                return kind;
        }

        return Text;
    }
}
