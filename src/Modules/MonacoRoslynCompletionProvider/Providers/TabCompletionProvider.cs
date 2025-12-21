using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Tags;
using MonacoRoslynCompletionProvider.Dto;

namespace MonacoRoslynCompletionProvider.Providers;

internal class TabCompletionProvider
{
    // Thanks to https://www.strathweb.com/2018/12/using-roslyn-c-completion-service-programmatically/
    public async Task<TabCompletionResult[]> Provide(Document document, int position)
    {
        var completionService = CompletionService.GetService(document);
        var results = await completionService.GetCompletionsAsync(document, position);

        var tabCompletionDTOs = new TabCompletionResult[results.ItemsList.Count];

        if (results != null)
        {
            var suggestions = new string[results.ItemsList.Count];

            for (int i = 0; i < results.ItemsList.Count; i++)
            {
                var itemDescription = await completionService.GetDescriptionAsync(document, results.ItemsList[i]);

                var completionItem = results.ItemsList[i];

                var dto = new TabCompletionResult
                {
                    Suggestion = completionItem.DisplayText,
                    Description = itemDescription.Text,
                    Kind = (int)MySemantic.itemkind_to_monaco[getCompletionItemKind(completionItem.Tags)]
                };

                tabCompletionDTOs[i] = dto;
                suggestions[i] = results.ItemsList[i].DisplayText;
            }

            return tabCompletionDTOs;
        }
        else
        {
            return Array.Empty<TabCompletionResult>();
        }
    }

    static CompletionItemKind getCompletionItemKind(ImmutableArray<string> tags)
    {
        foreach (var tag in tags)
        {
            if (s_roslynTagToCompletionItemKind.TryGetValue(tag, out var itemKind))
            {
                return itemKind;
            }
        }

        return CompletionItemKind.Text;
    }

    private static readonly Dictionary<string, CompletionItemKind> s_roslynTagToCompletionItemKind = new()
    {
        { WellKnownTags.Public, CompletionItemKind.Keyword },
        { WellKnownTags.Protected, CompletionItemKind.Keyword },
        { WellKnownTags.Private, CompletionItemKind.Keyword },
        { WellKnownTags.Internal, CompletionItemKind.Keyword },
        { WellKnownTags.File, CompletionItemKind.File },
        { WellKnownTags.Project, CompletionItemKind.File },
        { WellKnownTags.Folder, CompletionItemKind.Folder },
        { WellKnownTags.Assembly, CompletionItemKind.File },
        { WellKnownTags.Class, CompletionItemKind.Class },
        { WellKnownTags.Constant, CompletionItemKind.Constant },
        { WellKnownTags.Delegate, CompletionItemKind.Function },
        { WellKnownTags.Enum, CompletionItemKind.Enum },
        { WellKnownTags.EnumMember, CompletionItemKind.EnumMember },
        { WellKnownTags.Event, CompletionItemKind.Event },
        { WellKnownTags.ExtensionMethod, CompletionItemKind.Method },
        { WellKnownTags.Field, CompletionItemKind.Field },
        { WellKnownTags.Interface, CompletionItemKind.Interface },
        { WellKnownTags.Intrinsic, CompletionItemKind.Text },
        { WellKnownTags.Keyword, CompletionItemKind.Keyword },
        { WellKnownTags.Label, CompletionItemKind.Text },
        { WellKnownTags.Local, CompletionItemKind.Variable },
        { WellKnownTags.Namespace, CompletionItemKind.Module },
        { WellKnownTags.Method, CompletionItemKind.Method },
        { WellKnownTags.Module, CompletionItemKind.Module },
        { WellKnownTags.Operator, CompletionItemKind.Operator },
        { WellKnownTags.Parameter, CompletionItemKind.Value },
        { WellKnownTags.Property, CompletionItemKind.Property },
        { WellKnownTags.RangeVariable, CompletionItemKind.Variable },
        { WellKnownTags.Reference, CompletionItemKind.Reference },
        { WellKnownTags.Structure, CompletionItemKind.Struct },
        { WellKnownTags.TypeParameter, CompletionItemKind.TypeParameter },
        { WellKnownTags.Snippet, CompletionItemKind.Snippet },
        { WellKnownTags.Error, CompletionItemKind.Text },
        { WellKnownTags.Warning, CompletionItemKind.Text },
    };
}

public enum CompletionItemKind
{
    Text = 1,
    Method = 2,
    Function = 3,
    Constructor = 4,
    Field = 5,
    Variable = 6,
    Class = 7,
    Interface = 8,
    Module = 9,
    Property = 10,
    Unit = 11,
    Value = 12,
    Enum = 13,
    Keyword = 14,
    Snippet = 15,
    Color = 16,
    File = 17,
    Reference = 18,
    Folder = 19,
    EnumMember = 20,
    Constant = 21,
    Struct = 22,
    Event = 23,
    Operator = 24,
    TypeParameter = 25,
}
