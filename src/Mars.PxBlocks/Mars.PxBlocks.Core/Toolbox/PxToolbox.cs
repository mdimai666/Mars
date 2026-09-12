using System.Text.Json.Nodes;

namespace Mars.PxBlocks.Core.Toolbox;

/// <summary>
/// Модель toolbox. Сериализуется в JSON-формат Blockly (kind: categoryToolbox/flyoutToolbox).
/// </summary>
public class PxToolbox
{
    public List<PxToolboxItem> Contents { get; set; } = [];

    /// <summary>
    /// Доменные категории — перед разделителем и «Переменные»/«Функции»,
    /// как категории расширений в MakeCode.
    /// </summary>
    public void InsertDomainCategories(IReadOnlyList<PxToolboxCategory> categories)
    {
        if (categories.Count == 0)
            return;

        var index = Contents.FindIndex(item => item is PxToolboxSeparator);
        if (index < 0)
            index = Contents.Count;
        Contents.InsertRange(index, categories);
    }

    public string ToJson()
    {
        var root = new JsonObject
        {
            ["kind"] = Contents.Any(i => i is PxToolboxCategory) ? "categoryToolbox" : "flyoutToolbox",
            ["contents"] = new JsonArray(Contents.Select(i => i.ToJsonNode()).Cast<JsonNode?>().ToArray()),
        };
        return root.ToJsonString();
    }
}
