using Mars.PxBlocks.Core.Definitions;
using Mars.PxBlocks.Core.Toolbox;
using Mars.PxBlocks.Runtime.Execution;
using Mars.PxBlocks.Runtime.Parsing;

namespace Mars.PxBlocks.Tests;

/// <summary>
/// Согласованность трёх источников typeId: определения (PxStandardBlocks + события),
/// реализации исполнения (дефолтный локатор Runtime) и toolbox (PxDefaultToolbox).
/// Рассинхрон иначе виден только в браузере — блок в рейке без определения или
/// «No implementation registered» при запуске.
/// </summary>
public class PxTypeIdConsistencyTests
{
    /// <summary>Штатные блоки Blockly: определения и реализации даёт lists_* из Blockly (лейблы MakeCode — JsSrc/index.ts).</summary>
    private static readonly string[] BlocklyBuiltinTypes =
    [
        "lists_create_empty", "lists_create_with", "lists_repeat", "lists_length"
    ];

    /// <summary>Блоки, которые парсятся в узлы ядра и IPxBlockImplement не имеют.</summary>
    private static readonly string[] StructuralTypes =
    [
        PxCoreBlocks.If, PxCoreBlocks.IfElse,
        PxCoreBlocks.RepeatExt, PxCoreBlocks.WhileUntil, PxCoreBlocks.For, PxCoreBlocks.ForEach,
        PxCoreBlocks.FlowStatements,
        PxCoreBlocks.VariablesGet, PxCoreBlocks.VariablesSet, PxCoreBlocks.VariablesChange,
        PxCoreBlocks.LogicOperation, PxCoreBlocks.LogicTernary, PxCoreBlocks.LogicNull,
        PxCoreBlocks.ProceduresDefNoReturn, PxCoreBlocks.ProceduresDefReturn,
        PxCoreBlocks.ProceduresCallNoReturn, PxCoreBlocks.ProceduresCallReturn,
        PxCoreBlocks.IfReturn, PxCoreBlocks.ProceduresReturn,
        PxCoreBlocks.FunctionsIfReturn,
        PxCoreBlocks.StartEvent, PxCoreBlocks.LoopEvent
    ];

    [Fact]
    public void Toolbox_EveryBlockHasDefinition()
    {
        var definitions = DefinitionTypes();
        var missing = ToolboxBlockTypes()
            .Where(type => !definitions.Contains(type) && !BlocklyBuiltinTypes.Contains(type, StringComparer.Ordinal))
            .ToArray();

        Assert.Empty(missing);
    }

    [Fact]
    public void Definitions_EveryBlockIsStructuralOrImplemented()
    {
        var locator = PxInterpreter.CreateDefaultImplements();
        var missing = DefinitionTypes()
            .Where(type => !StructuralTypes.Contains(type, StringComparer.Ordinal) && !locator.Knows(type))
            .ToArray();

        Assert.Empty(missing);
    }

    [Fact]
    public void Implementations_EveryTypeHasDefinition()
    {
        var definitions = DefinitionTypes();
        var missing = PxInterpreter.CreateDefaultImplements().TypeIds
            .Where(type => !definitions.Contains(type) && !BlocklyBuiltinTypes.Contains(type, StringComparer.Ordinal))
            .ToArray();

        Assert.Empty(missing);
    }

    private static HashSet<string> DefinitionTypes()
    {
        var types = new PxStandardBlocks().Definitions.Select(d => d.TypeId).ToHashSet(StringComparer.Ordinal);
        types.Add(PxEventBlocks.CreateStart().TypeId);
        types.Add(PxEventBlocks.CreateLoop().TypeId);
        return types;
    }

    private static IEnumerable<string> ToolboxBlockTypes() =>
        PxDefaultToolbox.Create().Contents
            .OfType<PxToolboxCategory>()
            .SelectMany(category => category.Items.OfType<PxToolboxBlock>())
            .Select(block => block.Type);
}
