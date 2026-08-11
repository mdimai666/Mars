using System.Text.Json.Nodes;

namespace Mars.PxBlocks.Shared.Toolbox;

public abstract class PxToolboxItem
{
    internal abstract JsonNode ToJsonNode();
}
