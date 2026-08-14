using Mars.PxBlocks.Runtime.Execution;
using Mars.PxBlocks.Runtime.Parsing;

namespace Test.Mars.PxBlocks;

/// <summary>
/// Событийные блоки Start/Loop (аналог Arduino setup()/loop()) и режимы запуска:
/// по умолчанию — все верхнеуровневые; по именам — только переданные события.
/// Программа-фикстура: start печатает «setup», loop считает до трёх и выходит по break,
/// отдельный стек печатает «done» — loop обязан отложить его до «после всех».
/// </summary>
public class PxEventTests
{
    private const string ProgramJson = """
    {
      "blocks": { "languageVersion": 0, "blocks": [
        { "type": "px_start", "id": "start1",
          "inputs": { "DO": { "block":
            { "type": "text_print", "id": "ps",
              "inputs": { "TEXT": { "block": { "type": "text", "id": "ts", "fields": { "TEXT": "setup" } } } } }
          } }
        },
        { "type": "px_loop", "id": "loop1",
          "inputs": { "DO": { "block":
            { "type": "variables_set", "id": "inc",
              "fields": { "VAR": { "id": "varX" } },
              "inputs": { "VALUE": { "block": {
                "type": "math_arithmetic", "id": "add", "fields": { "OP": "ADD" },
                "inputs": {
                  "A": { "block": { "type": "variables_get", "id": "gx", "fields": { "VAR": { "id": "varX" } } } },
                  "B": { "block": { "type": "math_number", "id": "one", "fields": { "NUM": 1 } } }
                }
              } } },
              "next": { "block":
                { "type": "text_print", "id": "px",
                  "inputs": { "TEXT": { "block": { "type": "variables_get", "id": "gx2", "fields": { "VAR": { "id": "varX" } } } } },
                  "next": { "block":
                    { "type": "controls_if", "id": "ifb",
                      "inputs": {
                        "IF0": { "block": {
                          "type": "logic_compare", "id": "cmp", "fields": { "OP": "EQ" },
                          "inputs": {
                            "A": { "block": { "type": "variables_get", "id": "gx3", "fields": { "VAR": { "id": "varX" } } } },
                            "B": { "block": { "type": "math_number", "id": "three", "fields": { "NUM": 3 } } }
                          }
                        } },
                        "DO0": { "block": { "type": "controls_flow_statements", "id": "brk", "fields": { "FLOW": "BREAK" } } }
                      }
                    }
                  }
                }
              }
            }
          } }
        },
        { "type": "text_print", "id": "pd",
          "inputs": { "TEXT": { "block": { "type": "text", "id": "td", "fields": { "TEXT": "done" } } } } }
      ] },
      "variables": [ { "id": "varX", "name": "x" } ]
    }
    """;

    [Fact]
    public async Task DefaultMode_StartThenStacksThenLoop()
    {
        var result = await PxTestRun.RunAsync(ProgramJson, new PxRunOptions { YieldEvery = 0 });

        Assert.True(result.Success);
        Assert.Equal(["setup", "done", "1", "2", "3"], result.Output);
    }

    [Fact]
    public async Task EventNames_OnlyStart_SkipsStacksAndLoop()
    {
        var options = new PxRunOptions { YieldEvery = 0, EventNames = [global::Mars.PxBlocks.Runtime.Ast.PxEvents.Start] };
        var result = await PxTestRun.RunAsync(ProgramJson, options);

        Assert.True(result.Success);
        Assert.Equal(["setup"], result.Output);
    }

    [Fact]
    public async Task EventNames_OnlyLoop_SkipsStartAndStacks()
    {
        var options = new PxRunOptions { YieldEvery = 0, EventNames = [global::Mars.PxBlocks.Runtime.Ast.PxEvents.Loop] };
        var result = await PxTestRun.RunAsync(ProgramJson, options);

        Assert.True(result.Success);
        Assert.Equal(["1", "2", "3"], result.Output);
    }

    [Fact]
    public async Task EventNames_EmptyList_RunsNothing()
    {
        var options = new PxRunOptions { YieldEvery = 0, EventNames = [] };
        var result = await PxTestRun.RunAsync(ProgramJson, options);

        Assert.True(result.Success);
        Assert.Empty(result.Output);
    }

    [Fact]
    public async Task Loop_WithoutBreak_StopsAtStepLimit()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            { "type": "px_loop", "id": "loop1",
              "inputs": { "DO": { "block":
                { "type": "text_print", "id": "pt",
                  "inputs": { "TEXT": { "block": { "type": "text", "id": "tt", "fields": { "TEXT": "tick" } } } } }
              } }
            }
          ] }
        }
        """;

        var result = await PxTestRun.RunAsync(json, new PxRunOptions { StepLimit = 20, YieldEvery = 0 });

        Assert.False(result.Success);
        Assert.Equal("pt", result.ErrorBlockId); // лимит превышен на блоке тела цикла
        Assert.Contains("лимит", result.ErrorMessage);
    }

    [Fact]
    public void Parse_EventBlocks_RoutedToEventNodes()
    {
        var program = PxParser.CreateDefault().Parse(ProgramJson);

        var events = program.TopLevel.OfType<global::Mars.PxBlocks.Runtime.Ast.PxEventBlock>().ToList();
        Assert.Equal(2, events.Count);
        Assert.Equal(global::Mars.PxBlocks.Runtime.Ast.PxEvents.Start, events[0].EventName);
        Assert.Equal(global::Mars.PxBlocks.Runtime.Ast.PxEvents.Loop, events[1].EventName);
        Assert.NotNull(events[0].Body);
    }

    /// <summary>Loop стоит ПЕРВЫМ в workspace — фазы всё равно следуют порядку списка имён.</summary>
    private const string LoopFirstJson = """
    {
      "blocks": { "languageVersion": 0, "blocks": [
        { "type": "px_loop", "id": "loop1",
          "inputs": { "DO": { "block":
            { "type": "text_print", "id": "pl",
              "inputs": { "TEXT": { "block": { "type": "text", "id": "tl", "fields": { "TEXT": "L" } } } },
              "next": { "block": { "type": "controls_flow_statements", "id": "brk", "fields": { "FLOW": "BREAK" } } }
            }
          } }
        },
        { "type": "px_start", "id": "start1",
          "inputs": { "DO": { "block":
            { "type": "text_print", "id": "ps",
              "inputs": { "TEXT": { "block": { "type": "text", "id": "ts", "fields": { "TEXT": "S" } } } } }
          } }
        }
      ] }
    }
    """;

    [Theory]
    [InlineData("loop", "start", "L", "S")] // порядок фаз задаёт список, не workspace
    [InlineData("start", "loop", "S", "L")] // Loop гарантированно после Start
    public async Task EventNames_PhasesFollowListOrder(string first, string second, string expectedFirst, string expectedSecond)
    {
        var options = new PxRunOptions { YieldEvery = 0, EventNames = [first, second] };
        var result = await PxTestRun.RunAsync(LoopFirstJson, options);

        Assert.True(result.Success);
        Assert.Equal([expectedFirst, expectedSecond], result.Output);
    }

    [Fact]
    public async Task DefaultMode_LoopAlwaysAfterStart_EvenWhenLoopIsFirstInWorkspace()
    {
        var result = await PxTestRun.RunAsync(LoopFirstJson, new PxRunOptions { YieldEvery = 0 });

        Assert.True(result.Success);
        Assert.Equal(["S", "L"], result.Output);
    }
}
