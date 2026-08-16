using Mars.PxBlocks.Runtime.Execution;
using Mars.PxBlocks.Runtime.Parsing;

namespace Test.Mars.PxBlocks;

/// <summary>Хелпер: Blockly JSON → парсер → интерпретатор (локатор можно расширять).</summary>
internal static class PxTestRun
{
    public static Task<PxExecutionResult> RunAsync(
        string blocksJson,
        PxRunOptions? options = null,
        PxBlockImplementsLocator? locator = null,
        CancellationToken cancellationToken = default)
    {
        locator ??= PxInterpreter.CreateDefaultImplements();
        var program = new PxParser(locator).Parse(blocksJson);
        return new PxInterpreter(locator).RunAsync(program, options, cancellationToken);
    }

    /// <summary>Дефолтные опции тестов: без уступок потоку, воспроизводимый random.</summary>
    public static PxRunOptions Fast(PxRunOptions? options = null)
        => options ?? new PxRunOptions { YieldEvery = 0, RandomSeed = 1 };
}

public class PxParserTests
{
    [Fact]
    public void Parse_UnknownBlock_ThrowsWithBlockId()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            { "type": "no_such_block", "id": "bad1" }
          ] }
        }
        """;

        var exception = Assert.Throws<PxParseException>(() => PxParser.CreateDefault().Parse(json));
        Assert.Equal("bad1", exception.BlockId);
        Assert.Contains("no_such_block", exception.Message);
    }

    [Fact]
    public void Parse_DisabledTopLevelBlock_Skipped()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            { "type": "core.text.print", "id": "p1", "disabledReasons": ["manually disabled"],
              "inputs": { "TEXT": { "block": { "type": "core.text.text", "id": "t1", "fields": { "TEXT": "hi" } } } } }
          ] }
        }
        """;

        var program = PxParser.CreateDefault().Parse(json);
        Assert.Empty(program.TopLevel);
    }

    [Fact]
    public void Parse_IfExtraState_BuildsBranchesAndElse()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "core.logic.if", "id": "if1",
              "extraState": { "elseIfCount": 1, "hasElse": true },
              "inputs": {
                "IF0": { "block": { "type": "core.logic.boolean", "id": "b0", "fields": { "BOOL": "FALSE" } } },
                "IF1": { "block": { "type": "core.logic.boolean", "id": "b1", "fields": { "BOOL": "FALSE" } } },
                "DO0": { "block": { "type": "core.text.print", "id": "d0",
                  "inputs": { "TEXT": { "block": { "type": "core.text.text", "id": "t0", "fields": { "TEXT": "0" } } } } } },
                "DO1": { "block": { "type": "core.text.print", "id": "d1",
                  "inputs": { "TEXT": { "block": { "type": "core.text.text", "id": "t1", "fields": { "TEXT": "1" } } } } } },
                "ELSE": { "block": { "type": "core.text.print", "id": "de",
                  "inputs": { "TEXT": { "block": { "type": "core.text.text", "id": "te", "fields": { "TEXT": "else" } } } } } }
              }
            }
          ] }
        }
        """;

        var program = PxParser.CreateDefault().Parse(json);
        var ifStatement = (global::Mars.PxBlocks.Runtime.Ast.PxIfStatement)program.TopLevel.Single();
        Assert.Equal(2, ifStatement.Branches.Count);
        Assert.NotNull(ifStatement.ElseBody);
    }

    [Fact]
    public void Parse_ProcedureDef_CollectsNameAndParams()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "procedures_defnoreturn", "id": "def1",
              "fields": { "NAME": "demo" },
              "extraState": { "params": [ { "id": "p1", "name": "a" }, { "id": "p2", "name": "b" } ] }
            }
          ] }
        }
        """;

        var program = PxParser.CreateDefault().Parse(json);
        Assert.Empty(program.TopLevel);
        var procedure = Assert.Single(program.Procedures);
        Assert.Equal("demo", procedure.Name);
        Assert.Equal(["p1", "p2"], procedure.Params.Select(p => p.Id));
    }

    [Fact]
    public void Parse_VariableField_LegacyStringId()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            { "type": "core.variables.set", "id": "s1", "fields": { "VAR": "varX" },
              "inputs": { "VALUE": { "block": { "type": "core.math.number", "id": "n1", "fields": { "NUM": 1 } } } } }
          ] },
          "variables": [ { "id": "varX", "name": "x" } ]
        }
        """;

        var program = PxParser.CreateDefault().Parse(json);
        var set = (global::Mars.PxBlocks.Runtime.Ast.PxVariableSet)program.TopLevel.Single();
        Assert.Equal("varX", set.VariableId);
    }

    [Fact]
    public void Parse_StatementChain_FollowsNext()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            { "type": "core.text.print", "id": "p1",
              "inputs": { "TEXT": { "block": { "type": "core.text.text", "id": "t1", "fields": { "TEXT": "1" } } } },
              "next": { "block": { "type": "core.text.print", "id": "p2",
                "inputs": { "TEXT": { "block": { "type": "core.text.text", "id": "t2", "fields": { "TEXT": "2" } } } } } } }
          ] }
        }
        """;

        var program = PxParser.CreateDefault().Parse(json);
        var head = Assert.Single(program.TopLevel);
        Assert.Equal("p1", head.BlockId);
        Assert.NotNull(head.Next);
        Assert.Equal("p2", head.Next!.BlockId);
    }
}
