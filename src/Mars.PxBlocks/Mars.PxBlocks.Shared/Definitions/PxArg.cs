using System.Text.Json.Nodes;

namespace Mars.PxBlocks.Shared.Definitions;

public abstract class PxArg
{
    /// <summary>Имя аргумента в терминах Blockly; им же ссылается плейсхолдер {имя} в сообщении.</summary>
    public abstract string Name { get; set; }

    internal abstract JsonNode ToJsonNode();
}
