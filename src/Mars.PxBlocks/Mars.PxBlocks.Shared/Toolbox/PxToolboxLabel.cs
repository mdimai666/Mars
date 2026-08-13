using System.Text.Json.Nodes;

namespace Mars.PxBlocks.Shared.Toolbox;

/// <summary>
/// Текстовая метка во flyout. <see cref="WebClass"/> = "blocklyFlyoutHeading" —
/// заголовок раздела в стиле MakeCode (крупный текст, см. pxblocks.css).
/// </summary>
public class PxToolboxLabel : PxToolboxItem
{
    public string Text { get; set; } = "";

    /// <summary>CSS-класс кнопки flyout (Blockly web-class), напр. blocklyFlyoutHeading.</summary>
    public string? WebClass { get; set; }

    internal override JsonNode ToJsonNode()
    {
        var node = new JsonObject { ["kind"] = "label", ["text"] = Text };
        if (!string.IsNullOrEmpty(WebClass))
            node["web-class"] = WebClass;
        return node;
    }
}
