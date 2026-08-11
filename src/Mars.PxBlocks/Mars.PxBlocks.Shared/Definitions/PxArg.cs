using System.Text.Json.Nodes;

namespace Mars.PxBlocks.Shared.Definitions;

public abstract class PxArg
{
    internal abstract JsonNode ToJsonNode();
}
