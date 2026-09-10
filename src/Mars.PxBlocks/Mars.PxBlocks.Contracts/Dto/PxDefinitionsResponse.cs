using Mars.PxBlocks.Core.Toolbox;

namespace Mars.PxBlocks.Contracts.Dto;

/// <summary>
/// Определения блоков сервера (GET api/PxBlocks/Definitions): Blockly-JSON массив
/// определений (редактор передаёт его в registerBlockDefinitions без разбора)
/// + модель toolbox для рейки редактора.
/// </summary>
public sealed record PxDefinitionsResponse
{
    public required string DefinitionsJson { get; init; }

    public required PxToolbox Toolbox { get; init; }
}
