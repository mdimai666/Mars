using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Mars.PxBlocks.Shared.Toolbox;

/// <summary>
/// Элемент toolbox. Полиморфная сериализация STJ — для доставки toolbox
/// с сервера (api/PxBlocks/Definitions) в редактор.
/// </summary>
[JsonPolymorphic]
[JsonDerivedType(typeof(PxToolboxCategory), nameof(PxToolboxCategory))]
[JsonDerivedType(typeof(PxToolboxBlock), nameof(PxToolboxBlock))]
[JsonDerivedType(typeof(PxToolboxLabel), nameof(PxToolboxLabel))]
[JsonDerivedType(typeof(PxToolboxSeparator), nameof(PxToolboxSeparator))]
public abstract class PxToolboxItem
{
    internal abstract JsonNode ToJsonNode();
}
