using System.Collections.Frozen;
using System.Text;
using System.Text.Json;
using EditorJsBlazored.Blocks;

namespace EditorJsBlazored.Core;

public static class EditorTools
{
    public static readonly IReadOnlyDictionary<string, Type> RegisteredBlocks = new Dictionary<string, Type>()
    {
        ["paragraph"] = typeof(BlockParagraph),
        ["header"] = typeof(BlockHeader),
        ["linkTool"] = typeof(BlockLinkTool),
        ["image"] = typeof(BlockImage),
        ["list"] = typeof(BlockList),
        ["checklist"] = typeof(BlockChecklist),
        ["quote"] = typeof(BlockQuote),
        ["warning"] = typeof(BlockWarning),
        //["marker"] = typeof(marker), only tool
        ["code"] = typeof(BlockCode),
        ["delimiter"] = typeof(BlockDelimiter),
        //["inlineCode"] = typeof(inlineCode), only tool
        //["linkTool"] = typeof(linkTool), only tool
        ["embed"] = typeof(BlockEmbed),
        ["table"] = typeof(BlockTable),
        ["raw"] = typeof(BlockRaw),
        ["attaches"] = typeof(BlockAttaches),

    }.ToFrozenDictionary();

    public static Type ResolveBlockByType(string blockType)
    {
        return RegisteredBlocks.GetValueOrDefault(blockType) ?? throw new NotImplementedException($"blockType '{blockType}' not implemented");
    }

    /// <summary>
    /// RenderToHtml
    /// </summary>
    /// <param name="blocksJson">
    /// <code>
    /// { blocks: [
    ///     { type: 'paragraph', text: "123" }
    ///  ]}
    /// </code>
    /// </param>
    /// <returns>html content</returns>
    public static string RenderToHtml(string blocksJson)
    {
        var editorContent = JsonSerializer.Deserialize<EditorJsContent>(blocksJson, EditorJsContent._jsonSerializerOptions)!;
        return RenderToHtml(editorContent);
    }

    public static string RenderToHtml(EditorJsContent editorContent)
    {

        var sb = new StringBuilder();

        foreach (var block in editorContent.Blocks)
        {
            var blockTypeName = block.Type;
            var blockType = ResolveBlockByType(blockTypeName);
            //var deserializedBlock = (IEditorJsBlock)block.Data.Deserialize(blockType, _jsonSerializerOptions)!;
            var deserializedBlock = (IEditorJsBlock)JsonSerializer.SerializeToNode(block.Data).Deserialize(blockType, EditorJsContent._jsonSerializerOptions)!;
            var html = deserializedBlock.GetHtml();
            sb.AppendLine(html);
        }

        return sb.ToString();
    }

}
