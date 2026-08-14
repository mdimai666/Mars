using Mars.PxBlocks.Runtime.Execution;
using Mars.PxBlocks.Runtime.Values;

namespace Test.Mars.PxBlocks;

/// <summary>Зонд: лист, который отмечает своё вычисление в выводе (для short-circuit тестов).</summary>
internal sealed class ProbeExpressionImplement : IPxExpressionImplement
{
    public string TypeId => "test_probe";

    public ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
    {
        context.Print("probe");
        return ValueTask.FromResult<PxValue>(PxBooleanValue.True);
    }
}

public class PxInterpreterTests
{
    private static PxBlockImplementsLocator LocatorWithProbe()
    {
        var locator = PxInterpreter.CreateDefaultImplements();
        locator.Register(new ProbeExpressionImplement());
        return locator;
    }

    [Fact]
    public async Task SetVariable_And_Print_Arithmetic()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "variables_set", "id": "set1",
              "fields": { "VAR": { "id": "varX" } },
              "inputs": { "VALUE": { "block": {
                "type": "math_arithmetic", "id": "add1", "fields": { "OP": "ADD" },
                "inputs": {
                  "A": { "block": { "type": "math_number", "id": "n2", "fields": { "NUM": 2 } } },
                  "B": { "block": { "type": "math_number", "id": "n3", "fields": { "NUM": 3 } } }
                }
              } } },
              "next": { "block": {
                "type": "text_print", "id": "print1",
                "inputs": { "TEXT": { "block": { "type": "variables_get", "id": "getX", "fields": { "VAR": { "id": "varX" } } } } }
              } }
            }
          ] },
          "variables": [ { "id": "varX", "name": "x" } ]
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast());

        Assert.True(result.Success);
        Assert.Equal(["5"], result.Output);
    }

    [Theory]
    [InlineData("TRUE", "yes")]
    [InlineData("FALSE", "no")]
    public async Task IfElse_ChoosesBranch(string boolField, string expected)
    {
        var json = $$"""
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "controls_if_else", "id": "if1",
              "inputs": {
                "IF0": { "block": { "type": "logic_boolean", "id": "b", "fields": { "BOOL": "{{boolField}}" } } },
                "DO0": { "block": { "type": "text_print", "id": "py",
                  "inputs": { "TEXT": { "block": { "type": "text", "id": "ty", "fields": { "TEXT": "yes" } } } } } },
                "ELSE": { "block": { "type": "text_print", "id": "pn",
                  "inputs": { "TEXT": { "block": { "type": "text", "id": "tn", "fields": { "TEXT": "no" } } } } } }
              }
            }
          ] }
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast());

        Assert.True(result.Success);
        Assert.Equal([expected], result.Output);
    }

    [Fact]
    public async Task While_CounterToFive()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "variables_set", "id": "init",
              "fields": { "VAR": { "id": "varX" } },
              "inputs": { "VALUE": { "block": { "type": "math_number", "id": "zero", "fields": { "NUM": 0 } } } },
              "next": { "block": {
                "type": "controls_whileUntil", "id": "while1",
                "fields": { "MODE": "WHILE" },
                "inputs": {
                  "BOOL": { "block": {
                    "type": "logic_compare", "id": "cmp1", "fields": { "OP": "LT" },
                    "inputs": {
                      "A": { "block": { "type": "variables_get", "id": "gx", "fields": { "VAR": { "id": "varX" } } } },
                      "B": { "block": { "type": "math_number", "id": "five", "fields": { "NUM": 5 } } }
                    }
                  } },
                  "DO": { "block": {
                    "type": "variables_set", "id": "inc",
                    "fields": { "VAR": { "id": "varX" } },
                    "inputs": { "VALUE": { "block": {
                      "type": "math_arithmetic", "id": "addx", "fields": { "OP": "ADD" },
                      "inputs": {
                        "A": { "block": { "type": "variables_get", "id": "gx2", "fields": { "VAR": { "id": "varX" } } } },
                        "B": { "block": { "type": "math_number", "id": "one", "fields": { "NUM": 1 } } }
                      }
                    } } }
                  } }
                },
                "next": { "block": {
                  "type": "text_print", "id": "p",
                  "inputs": { "TEXT": { "block": { "type": "variables_get", "id": "gx3", "fields": { "VAR": { "id": "varX" } } } } }
                } }
              } }
            }
          ] },
          "variables": [ { "id": "varX", "name": "x" } ]
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast());

        Assert.True(result.Success);
        Assert.Equal(["5"], result.Output);
    }

    [Fact]
    public async Task For_SumsOneToFour()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "variables_set", "id": "init",
              "fields": { "VAR": { "id": "varS" } },
              "inputs": { "VALUE": { "block": { "type": "math_number", "id": "zero", "fields": { "NUM": 0 } } } },
              "next": { "block": {
                "type": "controls_for", "id": "for1",
                "fields": { "VAR": { "id": "varI" } },
                "inputs": {
                  "FROM": { "block": { "type": "math_number", "id": "from", "fields": { "NUM": 1 } } },
                  "TO": { "block": { "type": "math_number", "id": "to", "fields": { "NUM": 4 } } },
                  "BY": { "block": { "type": "math_number", "id": "by", "fields": { "NUM": 1 } } },
                  "DO": { "block": {
                    "type": "variables_set", "id": "add",
                    "fields": { "VAR": { "id": "varS" } },
                    "inputs": { "VALUE": { "block": {
                      "type": "math_arithmetic", "id": "adds", "fields": { "OP": "ADD" },
                      "inputs": {
                        "A": { "block": { "type": "variables_get", "id": "gs", "fields": { "VAR": { "id": "varS" } } } },
                        "B": { "block": { "type": "variables_get", "id": "gi", "fields": { "VAR": { "id": "varI" } } } }
                      }
                    } } }
                  } }
                },
                "next": { "block": {
                  "type": "text_print", "id": "p",
                  "inputs": { "TEXT": { "block": { "type": "variables_get", "id": "gs2", "fields": { "VAR": { "id": "varS" } } } } }
                } }
              } }
            }
          ] },
          "variables": [ { "id": "varS", "name": "s" }, { "id": "varI", "name": "i" } ]
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast());

        Assert.True(result.Success);
        Assert.Equal(["10"], result.Output);
    }

    [Fact]
    public async Task Repeat_WithShadowTimes_UsesShadow()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "controls_repeat_ext", "id": "rep1",
              "inputs": {
                "TIMES": { "shadow": { "type": "math_number", "id": "sh2", "fields": { "NUM": 2 } } },
                "DO": { "block": { "type": "text_print", "id": "p",
                  "inputs": { "TEXT": { "block": { "type": "text", "id": "t", "fields": { "TEXT": "x" } } } } } }
              }
            }
          ] }
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast());

        Assert.True(result.Success);
        Assert.Equal(["x", "x"], result.Output);
    }

    [Fact]
    public async Task BreakAndContinue_InForLoop()
    {
        // для i от 1 до 10: если i = 3, продолжить; если i = 5, выйти; печать i → 1,2,4
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "controls_for", "id": "for1",
              "fields": { "VAR": { "id": "varI" } },
              "inputs": {
                "FROM": { "block": { "type": "math_number", "id": "from", "fields": { "NUM": 1 } } },
                "TO": { "block": { "type": "math_number", "id": "to", "fields": { "NUM": 10 } } },
                "BY": { "block": { "type": "math_number", "id": "by", "fields": { "NUM": 1 } } },
                "DO": { "block": {
                  "type": "controls_if", "id": "ifSkip",
                  "extraState": { "elseIfCount": 1 },
                  "inputs": {
                    "IF0": { "block": {
                      "type": "logic_compare", "id": "c3", "fields": { "OP": "EQ" },
                      "inputs": {
                        "A": { "block": { "type": "variables_get", "id": "gi1", "fields": { "VAR": { "id": "varI" } } } },
                        "B": { "block": { "type": "math_number", "id": "n3", "fields": { "NUM": 3 } } }
                      }
                    } },
                    "DO0": { "block": { "type": "controls_flow_statements", "id": "cont", "fields": { "FLOW": "CONTINUE" } } },
                    "IF1": { "block": {
                      "type": "logic_compare", "id": "c5", "fields": { "OP": "EQ" },
                      "inputs": {
                        "A": { "block": { "type": "variables_get", "id": "gi2", "fields": { "VAR": { "id": "varI" } } } },
                        "B": { "block": { "type": "math_number", "id": "n5", "fields": { "NUM": 5 } } }
                      }
                    } },
                    "DO1": { "block": { "type": "controls_flow_statements", "id": "brk", "fields": { "FLOW": "BREAK" } } }
                  },
                  "next": { "block": {
                    "type": "text_print", "id": "p",
                    "inputs": { "TEXT": { "block": { "type": "variables_get", "id": "gi3", "fields": { "VAR": { "id": "varI" } } } } }
                  } }
                } }
              }
            }
          ] },
          "variables": [ { "id": "varI", "name": "i" } ]
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast());

        Assert.True(result.Success);
        Assert.Equal(["1", "2", "4"], result.Output);
    }

    [Fact]
    public async Task Procedure_WithArgsAndReturn()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "procedures_defreturn", "id": "def1",
              "fields": { "NAME": "double" },
              "extraState": { "params": [ { "id": "pN", "name": "n" } ] },
              "inputs": {
                "RETURN": { "block": {
                  "type": "math_arithmetic", "id": "mul", "fields": { "OP": "MULTIPLY" },
                  "inputs": {
                    "A": { "block": { "type": "variables_get", "id": "gn", "fields": { "VAR": { "id": "pN" } } } },
                    "B": { "block": { "type": "math_number", "id": "two", "fields": { "NUM": 2 } } }
                  }
                } }
              }
            },
            {
              "type": "text_print", "id": "p1",
              "inputs": { "TEXT": { "block": {
                "type": "procedures_callreturn", "id": "call1",
                "extraState": { "name": "double", "params": [ "n" ] },
                "inputs": { "ARG0": { "block": { "type": "math_number", "id": "five", "fields": { "NUM": 5 } } } }
              } } }
            }
          ] }
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast());

        Assert.True(result.Success);
        Assert.Equal(["10"], result.Output);
    }

    [Fact]
    public async Task Procedure_Recursion_Factorial()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "procedures_defreturn", "id": "def1",
              "fields": { "NAME": "fact" },
              "extraState": { "params": [ { "id": "pN", "name": "n" } ] },
              "inputs": {
                "STACK": { "block": {
                  "type": "procedures_ifreturn", "id": "ifr",
                  "inputs": {
                    "CONDITION": { "block": {
                      "type": "logic_compare", "id": "c1", "fields": { "OP": "EQ" },
                      "inputs": {
                        "A": { "block": { "type": "variables_get", "id": "gn", "fields": { "VAR": { "id": "pN" } } } },
                        "B": { "block": { "type": "math_number", "id": "one", "fields": { "NUM": 1 } } }
                      }
                    } },
                    "VALUE": { "block": { "type": "math_number", "id": "rv", "fields": { "NUM": 1 } } }
                  }
                } },
                "RETURN": { "block": {
                  "type": "math_arithmetic", "id": "mul", "fields": { "OP": "MULTIPLY" },
                  "inputs": {
                    "A": { "block": { "type": "variables_get", "id": "gn2", "fields": { "VAR": { "id": "pN" } } } },
                    "B": { "block": {
                      "type": "procedures_callreturn", "id": "rec",
                      "extraState": { "name": "fact", "params": [ "n" ] },
                      "inputs": { "ARG0": { "block": {
                        "type": "math_arithmetic", "id": "sub", "fields": { "OP": "MINUS" },
                        "inputs": {
                          "A": { "block": { "type": "variables_get", "id": "gn3", "fields": { "VAR": { "id": "pN" } } } },
                          "B": { "block": { "type": "math_number", "id": "one2", "fields": { "NUM": 1 } } }
                        }
                      } } }
                    } }
                  }
                } }
              }
            },
            {
              "type": "text_print", "id": "p1",
              "inputs": { "TEXT": { "block": {
                "type": "procedures_callreturn", "id": "call1",
                "extraState": { "name": "fact", "params": [ "n" ] },
                "inputs": { "ARG0": { "block": { "type": "math_number", "id": "five", "fields": { "NUM": 5 } } } }
              } } }
            }
          ] }
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast());

        Assert.True(result.Success);
        Assert.Equal(["120"], result.Output);
    }

    [Fact]
    public async Task Procedure_ParamShadowsGlobal()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "variables_set", "id": "init",
              "fields": { "VAR": { "id": "varX" } },
              "inputs": { "VALUE": { "block": { "type": "math_number", "id": "one", "fields": { "NUM": 1 } } } },
              "next": { "block": {
                "type": "procedures_callnoreturn", "id": "call1",
                "extraState": { "name": "f", "params": [ "x" ] },
                "inputs": { "ARG0": { "block": { "type": "math_number", "id": "five", "fields": { "NUM": 5 } } } },
                "next": { "block": {
                  "type": "text_print", "id": "p",
                  "inputs": { "TEXT": { "block": { "type": "variables_get", "id": "gx", "fields": { "VAR": { "id": "varX" } } } } }
                } }
              } }
            },
            {
              "type": "procedures_defnoreturn", "id": "def1",
              "fields": { "NAME": "f" },
              "extraState": { "params": [ { "id": "pX", "name": "x" } ] },
              "inputs": { "STACK": { "block": {
                "type": "variables_set", "id": "setp",
                "fields": { "VAR": { "id": "pX" } },
                "inputs": { "VALUE": { "block": { "type": "math_number", "id": "hundred", "fields": { "NUM": 100 } } } }
              } } }
            }
          ] },
          "variables": [ { "id": "varX", "name": "x" } ]
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast());

        Assert.True(result.Success);
        Assert.Equal(["1"], result.Output); // глобальный x не задет параметром
    }

    [Fact]
    public async Task LogicOperation_ShortCircuit_And()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "text_print", "id": "p1",
              "inputs": { "TEXT": { "block": {
                "type": "logic_operation", "id": "and1", "fields": { "OP": "AND" },
                "inputs": {
                  "A": { "block": { "type": "logic_boolean", "id": "f", "fields": { "BOOL": "FALSE" } } },
                  "B": { "block": { "type": "test_probe", "id": "probe1" } }
                }
              } } }
            }
          ] }
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast(), LocatorWithProbe());

        Assert.True(result.Success);
        Assert.Equal(["false"], result.Output); // зонд не вычислялся
    }

    [Fact]
    public async Task LogicTernary_DoesNotEvaluateUntakenBranch()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "text_print", "id": "p1",
              "inputs": { "TEXT": { "block": {
                "type": "logic_ternary", "id": "t1",
                "inputs": {
                  "IF": { "block": { "type": "logic_boolean", "id": "f", "fields": { "BOOL": "FALSE" } } },
                  "THEN": { "block": { "type": "test_probe", "id": "probe1" } },
                  "ELSE": { "block": { "type": "math_number", "id": "seven", "fields": { "NUM": 7 } } }
                }
              } } }
            }
          ] }
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast(), LocatorWithProbe());

        Assert.True(result.Success);
        Assert.Equal(["7"], result.Output);
    }

    [Fact]
    public async Task StepLimit_StopsInfiniteLoop()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "controls_whileUntil", "id": "w",
              "fields": { "MODE": "WHILE" },
              "inputs": {
                "BOOL": { "block": { "type": "logic_boolean", "id": "t", "fields": { "BOOL": "TRUE" } } }
              }
            }
          ] }
        }
        """;

        var result = await PxTestRun.RunAsync(json, new PxRunOptions { StepLimit = 100, YieldEvery = 0 });

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("лимит", result.ErrorMessage);
        Assert.Equal("w", result.ErrorBlockId);
    }

    [Fact]
    public async Task Cancellation_ReturnsCanceled()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "controls_whileUntil", "id": "w",
              "fields": { "MODE": "WHILE" },
              "inputs": {
                "BOOL": { "block": { "type": "logic_boolean", "id": "t", "fields": { "BOOL": "TRUE" } } }
              }
            }
          ] }
        }
        """;

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var result = await PxTestRun.RunAsync(json, new PxRunOptions { YieldEvery = 0 }, cancellationToken: cancellation.Token);

        Assert.True(result.Canceled);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ForEach_OverNonList_Errors()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "controls_forEach", "id": "fe",
              "fields": { "VAR": { "id": "varI" } },
              "inputs": {
                "LIST": { "block": { "type": "math_number", "id": "n", "fields": { "NUM": 1 } } }
              }
            }
          ] },
          "variables": [ { "id": "varI", "name": "i" } ]
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast());

        Assert.False(result.Success);
        Assert.Equal("fe", result.ErrorBlockId);
    }

    [Fact]
    public async Task Procedure_Undefined_Errors()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "procedures_callnoreturn", "id": "call1",
              "extraState": { "name": "missing", "params": [] }
            }
          ] }
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast());

        Assert.False(result.Success);
        Assert.Contains("missing", result.ErrorMessage);
    }

    [Fact]
    public async Task Events_StreamBlockEnteredAndOutput()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            { "type": "text_print", "id": "print1",
              "inputs": { "TEXT": { "block": { "type": "text", "id": "t", "fields": { "TEXT": "hi" } } } } }
          ] }
        }
        """;

        var events = new List<PxExecutionEvent>();
        var options = new PxRunOptions { YieldEvery = 0, OnEvent = events.Add };
        var result = await PxTestRun.RunAsync(json, options);

        Assert.True(result.Success);
        Assert.Contains(events, e => e.Kind == PxExecutionEventKind.BlockEntered && e.BlockId == "print1");
        Assert.Contains(events, e => e.Kind == PxExecutionEventKind.Output && e.Text == "hi");
    }
}
