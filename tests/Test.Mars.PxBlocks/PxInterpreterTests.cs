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
        locator.Register(typeof(ProbeExpressionImplement));
        return locator;
    }

    [Fact]
    public async Task SetVariable_And_Print_Arithmetic()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "core.variables.set", "id": "set1",
              "fields": { "VAR": { "id": "varX" } },
              "inputs": { "VALUE": { "block": {
                "type": "core.math.arithmetic", "id": "add1", "fields": { "OP": "ADD" },
                "inputs": {
                  "A": { "block": { "type": "core.math.number", "id": "n2", "fields": { "NUM": 2 } } },
                  "B": { "block": { "type": "core.math.number", "id": "n3", "fields": { "NUM": 3 } } }
                }
              } } },
              "next": { "block": {
                "type": "core.text.print", "id": "print1",
                "inputs": { "TEXT": { "block": { "type": "core.variables.get", "id": "getX", "fields": { "VAR": { "id": "varX" } } } } }
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
              "type": "core.logic.if_else", "id": "if1",
              "inputs": {
                "IF0": { "block": { "type": "core.logic.boolean", "id": "b", "fields": { "BOOL": "{{boolField}}" } } },
                "DO0": { "block": { "type": "core.text.print", "id": "py",
                  "inputs": { "TEXT": { "block": { "type": "core.text.text", "id": "ty", "fields": { "TEXT": "yes" } } } } } },
                "ELSE": { "block": { "type": "core.text.print", "id": "pn",
                  "inputs": { "TEXT": { "block": { "type": "core.text.text", "id": "tn", "fields": { "TEXT": "no" } } } } } }
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
              "type": "core.variables.set", "id": "init",
              "fields": { "VAR": { "id": "varX" } },
              "inputs": { "VALUE": { "block": { "type": "core.math.number", "id": "zero", "fields": { "NUM": 0 } } } },
              "next": { "block": {
                "type": "core.loops.while_until", "id": "while1",
                "fields": { "MODE": "WHILE" },
                "inputs": {
                  "BOOL": { "block": {
                    "type": "core.logic.compare", "id": "cmp1", "fields": { "OP": "LT" },
                    "inputs": {
                      "A": { "block": { "type": "core.variables.get", "id": "gx", "fields": { "VAR": { "id": "varX" } } } },
                      "B": { "block": { "type": "core.math.number", "id": "five", "fields": { "NUM": 5 } } }
                    }
                  } },
                  "DO": { "block": {
                    "type": "core.variables.set", "id": "inc",
                    "fields": { "VAR": { "id": "varX" } },
                    "inputs": { "VALUE": { "block": {
                      "type": "core.math.arithmetic", "id": "addx", "fields": { "OP": "ADD" },
                      "inputs": {
                        "A": { "block": { "type": "core.variables.get", "id": "gx2", "fields": { "VAR": { "id": "varX" } } } },
                        "B": { "block": { "type": "core.math.number", "id": "one", "fields": { "NUM": 1 } } }
                      }
                    } } }
                  } }
                },
                "next": { "block": {
                  "type": "core.text.print", "id": "p",
                  "inputs": { "TEXT": { "block": { "type": "core.variables.get", "id": "gx3", "fields": { "VAR": { "id": "varX" } } } } }
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
              "type": "core.variables.set", "id": "init",
              "fields": { "VAR": { "id": "varS" } },
              "inputs": { "VALUE": { "block": { "type": "core.math.number", "id": "zero", "fields": { "NUM": 0 } } } },
              "next": { "block": {
                "type": "core.loops.for", "id": "for1",
                "fields": { "VAR": { "id": "varI" } },
                "inputs": {
                  "FROM": { "block": { "type": "core.math.number", "id": "from", "fields": { "NUM": 1 } } },
                  "TO": { "block": { "type": "core.math.number", "id": "to", "fields": { "NUM": 4 } } },
                  "BY": { "block": { "type": "core.math.number", "id": "by", "fields": { "NUM": 1 } } },
                  "DO": { "block": {
                    "type": "core.variables.set", "id": "add",
                    "fields": { "VAR": { "id": "varS" } },
                    "inputs": { "VALUE": { "block": {
                      "type": "core.math.arithmetic", "id": "adds", "fields": { "OP": "ADD" },
                      "inputs": {
                        "A": { "block": { "type": "core.variables.get", "id": "gs", "fields": { "VAR": { "id": "varS" } } } },
                        "B": { "block": { "type": "core.variables.get", "id": "gi", "fields": { "VAR": { "id": "varI" } } } }
                      }
                    } } }
                  } }
                },
                "next": { "block": {
                  "type": "core.text.print", "id": "p",
                  "inputs": { "TEXT": { "block": { "type": "core.variables.get", "id": "gs2", "fields": { "VAR": { "id": "varS" } } } } }
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
              "type": "core.loops.repeat", "id": "rep1",
              "inputs": {
                "TIMES": { "shadow": { "type": "core.math.number", "id": "sh2", "fields": { "NUM": 2 } } },
                "DO": { "block": { "type": "core.text.print", "id": "p",
                  "inputs": { "TEXT": { "block": { "type": "core.text.text", "id": "t", "fields": { "TEXT": "x" } } } } } }
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
              "type": "core.loops.for", "id": "for1",
              "fields": { "VAR": { "id": "varI" } },
              "inputs": {
                "FROM": { "block": { "type": "core.math.number", "id": "from", "fields": { "NUM": 1 } } },
                "TO": { "block": { "type": "core.math.number", "id": "to", "fields": { "NUM": 10 } } },
                "BY": { "block": { "type": "core.math.number", "id": "by", "fields": { "NUM": 1 } } },
                "DO": { "block": {
                  "type": "core.logic.if", "id": "ifSkip",
                  "extraState": { "elseIfCount": 1 },
                  "inputs": {
                    "IF0": { "block": {
                      "type": "core.logic.compare", "id": "c3", "fields": { "OP": "EQ" },
                      "inputs": {
                        "A": { "block": { "type": "core.variables.get", "id": "gi1", "fields": { "VAR": { "id": "varI" } } } },
                        "B": { "block": { "type": "core.math.number", "id": "n3", "fields": { "NUM": 3 } } }
                      }
                    } },
                    "DO0": { "block": { "type": "core.loops.flow", "id": "cont", "fields": { "FLOW": "CONTINUE" } } },
                    "IF1": { "block": {
                      "type": "core.logic.compare", "id": "c5", "fields": { "OP": "EQ" },
                      "inputs": {
                        "A": { "block": { "type": "core.variables.get", "id": "gi2", "fields": { "VAR": { "id": "varI" } } } },
                        "B": { "block": { "type": "core.math.number", "id": "n5", "fields": { "NUM": 5 } } }
                      }
                    } },
                    "DO1": { "block": { "type": "core.loops.flow", "id": "brk", "fields": { "FLOW": "BREAK" } } }
                  },
                  "next": { "block": {
                    "type": "core.text.print", "id": "p",
                    "inputs": { "TEXT": { "block": { "type": "core.variables.get", "id": "gi3", "fields": { "VAR": { "id": "varI" } } } } }
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
                  "type": "core.math.arithmetic", "id": "mul", "fields": { "OP": "MULTIPLY" },
                  "inputs": {
                    "A": { "block": { "type": "core.variables.get", "id": "gn", "fields": { "VAR": { "id": "pN" } } } },
                    "B": { "block": { "type": "core.math.number", "id": "two", "fields": { "NUM": 2 } } }
                  }
                } }
              }
            },
            {
              "type": "core.text.print", "id": "p1",
              "inputs": { "TEXT": { "block": {
                "type": "procedures_callreturn", "id": "call1",
                "extraState": { "name": "double", "params": [ "n" ] },
                "inputs": { "ARG0": { "block": { "type": "core.math.number", "id": "five", "fields": { "NUM": 5 } } } }
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
                      "type": "core.logic.compare", "id": "c1", "fields": { "OP": "EQ" },
                      "inputs": {
                        "A": { "block": { "type": "core.variables.get", "id": "gn", "fields": { "VAR": { "id": "pN" } } } },
                        "B": { "block": { "type": "core.math.number", "id": "one", "fields": { "NUM": 1 } } }
                      }
                    } },
                    "VALUE": { "block": { "type": "core.math.number", "id": "rv", "fields": { "NUM": 1 } } }
                  }
                } },
                "RETURN": { "block": {
                  "type": "core.math.arithmetic", "id": "mul", "fields": { "OP": "MULTIPLY" },
                  "inputs": {
                    "A": { "block": { "type": "core.variables.get", "id": "gn2", "fields": { "VAR": { "id": "pN" } } } },
                    "B": { "block": {
                      "type": "procedures_callreturn", "id": "rec",
                      "extraState": { "name": "fact", "params": [ "n" ] },
                      "inputs": { "ARG0": { "block": {
                        "type": "core.math.arithmetic", "id": "sub", "fields": { "OP": "MINUS" },
                        "inputs": {
                          "A": { "block": { "type": "core.variables.get", "id": "gn3", "fields": { "VAR": { "id": "pN" } } } },
                          "B": { "block": { "type": "core.math.number", "id": "one2", "fields": { "NUM": 1 } } }
                        }
                      } } }
                    } }
                  }
                } }
              }
            },
            {
              "type": "core.text.print", "id": "p1",
              "inputs": { "TEXT": { "block": {
                "type": "procedures_callreturn", "id": "call1",
                "extraState": { "name": "fact", "params": [ "n" ] },
                "inputs": { "ARG0": { "block": { "type": "core.math.number", "id": "five", "fields": { "NUM": 5 } } } }
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
              "type": "core.variables.set", "id": "init",
              "fields": { "VAR": { "id": "varX" } },
              "inputs": { "VALUE": { "block": { "type": "core.math.number", "id": "one", "fields": { "NUM": 1 } } } },
              "next": { "block": {
                "type": "procedures_callnoreturn", "id": "call1",
                "extraState": { "name": "f", "params": [ "x" ] },
                "inputs": { "ARG0": { "block": { "type": "core.math.number", "id": "five", "fields": { "NUM": 5 } } } },
                "next": { "block": {
                  "type": "core.text.print", "id": "p",
                  "inputs": { "TEXT": { "block": { "type": "core.variables.get", "id": "gx", "fields": { "VAR": { "id": "varX" } } } } }
                } }
              } }
            },
            {
              "type": "procedures_defnoreturn", "id": "def1",
              "fields": { "NAME": "f" },
              "extraState": { "params": [ { "id": "pX", "name": "x" } ] },
              "inputs": { "STACK": { "block": {
                "type": "core.variables.set", "id": "setp",
                "fields": { "VAR": { "id": "pX" } },
                "inputs": { "VALUE": { "block": { "type": "core.math.number", "id": "hundred", "fields": { "NUM": 100 } } } }
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
    public async Task Procedure_Return_EarlyWithValue()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "procedures_defreturn", "id": "def1",
              "fields": { "NAME": "pick" },
              "inputs": {
                "STACK": { "block": {
                  "type": "core.text.print", "id": "p1",
                  "inputs": { "TEXT": { "block": { "type": "core.text.text", "id": "t1", "fields": { "TEXT": "before" } } } },
                  "next": { "block": {
                    "type": "procedures_return", "id": "ret",
                    "inputs": { "VALUE": { "block": { "type": "core.math.number", "id": "n42", "fields": { "NUM": 42 } } } },
                    "next": { "block": {
                      "type": "core.text.print", "id": "p2",
                      "inputs": { "TEXT": { "block": { "type": "core.text.text", "id": "t2", "fields": { "TEXT": "after" } } } }
                    } }
                  } }
                } }
              }
            },
            {
              "type": "core.text.print", "id": "p3",
              "inputs": { "TEXT": { "block": {
                "type": "procedures_callreturn", "id": "call1",
                "extraState": { "name": "pick", "params": [] }
              } } }
            }
          ] }
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast());

        Assert.True(result.Success);
        Assert.Equal(["before", "42"], result.Output); // «after» пропущено ранним return
    }

    [Fact]
    public async Task Procedure_Return_WithoutValue_GivesNull()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "procedures_defreturn", "id": "def1",
              "fields": { "NAME": "noval" },
              "inputs": {
                "STACK": { "block": { "type": "procedures_return", "id": "ret" } },
                "RETURN": { "block": { "type": "core.math.number", "id": "seven", "fields": { "NUM": 7 } } }
              }
            },
            {
              "type": "core.text.print", "id": "p1",
              "inputs": { "TEXT": { "block": {
                "type": "procedures_callreturn", "id": "call1",
                "extraState": { "name": "noval", "params": [] }
              } } }
            }
          ] }
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast());

        Assert.True(result.Success);
        Assert.Equal(["null"], result.Output); // return без значения, RETURN-слот не исполняется
    }

    [Fact]
    public async Task Return_AtTopLevel_EndsProgram()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "core.text.print", "id": "p1",
              "inputs": { "TEXT": { "block": { "type": "core.text.text", "id": "t1", "fields": { "TEXT": "1" } } } },
              "next": { "block": {
                "type": "procedures_return", "id": "ret",
                "next": { "block": {
                  "type": "core.text.print", "id": "p2",
                  "inputs": { "TEXT": { "block": { "type": "core.text.text", "id": "t2", "fields": { "TEXT": "2" } } } }
                } }
              } }
            }
          ] }
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast());

        Assert.True(result.Success); // return вне функции завершает программу
        Assert.Equal(["1"], result.Output);
    }

    [Fact]
    public async Task VariablesChange_NullStartsFromZero_ThenAdds()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "core.variables.change", "id": "c1",
              "fields": { "VAR": { "id": "varX" } },
              "inputs": { "DELTA": { "block": { "type": "core.math.number", "id": "d7", "fields": { "NUM": 7 } } } },
              "next": { "block": {
                "type": "core.text.print", "id": "p1",
                "inputs": { "TEXT": { "block": { "type": "core.variables.get", "id": "g1", "fields": { "VAR": { "id": "varX" } } } } },
                "next": { "block": {
                  "type": "core.variables.set", "id": "s1",
                  "fields": { "VAR": { "id": "varX" } },
                  "inputs": { "VALUE": { "block": { "type": "core.math.number", "id": "n10", "fields": { "NUM": 10 } } } },
                  "next": { "block": {
                    "type": "core.variables.change", "id": "c2",
                    "fields": { "VAR": { "id": "varX" } },
                    "inputs": { "DELTA": { "block": { "type": "core.math.number", "id": "d5", "fields": { "NUM": 5 } } } },
                    "next": { "block": {
                      "type": "core.text.print", "id": "p2",
                      "inputs": { "TEXT": { "block": { "type": "core.variables.get", "id": "g2", "fields": { "VAR": { "id": "varX" } } } } }
                    } }
                  } }
                } }
              } }
            }
          ] },
          "variables": [ { "id": "varX", "name": "x" } ]
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast());

        Assert.True(result.Success);
        Assert.Equal(["7", "15"], result.Output); // необъявленное значение — 0, затем 10+5
    }

    [Fact]
    public async Task LoopsPause_WaitsAndContinues()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "core.loops.pause", "id": "w",
              "inputs": { "MS": { "block": { "type": "core.math.number", "id": "n1", "fields": { "NUM": 1 } } } },
              "next": { "block": {
                "type": "core.text.print", "id": "p1",
                "inputs": { "TEXT": { "block": { "type": "core.text.text", "id": "t1", "fields": { "TEXT": "ok" } } } }
              } }
            }
          ] }
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast());

        Assert.True(result.Success);
        Assert.Equal(["ok"], result.Output);
    }

    [Fact]
    public async Task Math_MinMax()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "core.text.print", "id": "p1",
              "inputs": { "TEXT": { "block": {
                "type": "core.math.min_max", "id": "mn", "fields": { "OP": "MIN" },
                "inputs": {
                  "A": { "block": { "type": "core.math.number", "id": "a1", "fields": { "NUM": 3 } } },
                  "B": { "block": { "type": "core.math.number", "id": "b1", "fields": { "NUM": 5 } } }
                }
              } } },
              "next": { "block": {
                "type": "core.text.print", "id": "p2",
                "inputs": { "TEXT": { "block": {
                  "type": "core.math.min_max", "id": "mx", "fields": { "OP": "MAX" },
                  "inputs": {
                    "A": { "block": { "type": "core.math.number", "id": "a2", "fields": { "NUM": 3 } } },
                    "B": { "block": { "type": "core.math.number", "id": "b2", "fields": { "NUM": 5 } } }
                  }
                } } }
              } }
            }
          ] }
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast());

        Assert.True(result.Success);
        Assert.Equal(["3", "5"], result.Output);
    }

    [Fact]
    public async Task TextExtensions_Substring_Includes_Compare()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "core.text.print", "id": "p1",
              "inputs": { "TEXT": { "block": {
                "type": "core.text.substring", "id": "s1",
                "inputs": {
                  "VALUE": { "block": { "type": "core.text.text", "id": "t1", "fields": { "TEXT": "hello" } } },
                  "START": { "block": { "type": "core.math.number", "id": "n1", "fields": { "NUM": 1 } } },
                  "LENGTH": { "block": { "type": "core.math.number", "id": "n2", "fields": { "NUM": 3 } } }
                }
              } } },
              "next": { "block": {
                "type": "core.text.print", "id": "p2",
                "inputs": { "TEXT": { "block": {
                  "type": "core.text.substring", "id": "s2",
                  "inputs": {
                    "VALUE": { "block": { "type": "core.text.text", "id": "t2", "fields": { "TEXT": "hello" } } },
                    "START": { "block": { "type": "core.math.number", "id": "n3", "fields": { "NUM": -2 } } },
                    "LENGTH": { "block": { "type": "core.math.number", "id": "n4", "fields": { "NUM": 0 } } }
                  }
                } } },
                "next": { "block": {
                  "type": "core.text.print", "id": "p3",
                  "inputs": { "TEXT": { "block": {
                    "type": "core.text.includes", "id": "i1",
                    "inputs": {
                      "VALUE": { "block": { "type": "core.text.text", "id": "t3", "fields": { "TEXT": "hello" } } },
                      "FIND": { "block": { "type": "core.text.text", "id": "t4", "fields": { "TEXT": "ell" } } }
                    }
                  } } },
                  "next": { "block": {
                    "type": "core.text.print", "id": "p4",
                    "inputs": { "TEXT": { "block": {
                      "type": "core.text.compare", "id": "c1",
                      "inputs": {
                        "A": { "block": { "type": "core.text.text", "id": "t5", "fields": { "TEXT": "a" } } },
                        "B": { "block": { "type": "core.text.text", "id": "t6", "fields": { "TEXT": "b" } } }
                      }
                    } } }
                  } } }
                } }
              } }
          ] }
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast());

        Assert.True(result.Success);
        // старт 0-основный; -2 — с конца, длина 0 — до конца; compare — порядковый (-1)
        Assert.Equal(["ell", "lo", "true", "-1"], result.Output);
    }

    [Fact]
    public async Task TextExtensions_Split_Parse_CharCode()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "core.text.print", "id": "p1",
              "inputs": { "TEXT": { "block": {
                "type": "core.text.split", "id": "sp1",
                "inputs": {
                  "VALUE": { "block": { "type": "core.text.text", "id": "t1", "fields": { "TEXT": "a-b" } } },
                  "SEPARATOR": { "block": { "type": "core.text.text", "id": "t2", "fields": { "TEXT": "-" } } }
                }
              } } },
              "next": { "block": {
                "type": "core.text.print", "id": "p2",
                "inputs": { "TEXT": { "block": {
                  "type": "core.text.parse", "id": "pa1",
                  "inputs": { "VALUE": { "block": { "type": "core.text.text", "id": "t3", "fields": { "TEXT": "12abc" } } } }
                } } },
                "next": { "block": {
                  "type": "core.text.print", "id": "p3",
                  "inputs": { "TEXT": { "block": {
                    "type": "core.text.parse", "id": "pa2",
                    "inputs": { "VALUE": { "block": { "type": "core.text.text", "id": "t4", "fields": { "TEXT": "abc" } } } }
                  } } },
                  "next": { "block": {
                    "type": "core.text.print", "id": "p4",
                    "inputs": { "TEXT": { "block": {
                      "type": "core.text.char_code", "id": "cc1",
                      "inputs": {
                        "VALUE": { "block": { "type": "core.text.text", "id": "t5", "fields": { "TEXT": "A" } } },
                        "INDEX": { "block": { "type": "core.math.number", "id": "n1", "fields": { "NUM": 0 } } }
                      }
                    } } }
                  } } }
                } }
              } }
          ] }
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast());

        Assert.True(result.Success);
        Assert.Equal(["a,b", "12", "NaN", "65"], result.Output);
    }

    [Fact]
    public async Task Math_Map_ReMapsProportionally()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "core.text.print", "id": "p1",
              "inputs": { "TEXT": { "block": {
                "type": "core.math.map", "id": "m1",
                "inputs": {
                  "VALUE": { "block": { "type": "core.math.number", "id": "v", "fields": { "NUM": 512 } } },
                  "FROM_LOW": { "block": { "type": "core.math.number", "id": "fl", "fields": { "NUM": 0 } } },
                  "FROM_HIGH": { "block": { "type": "core.math.number", "id": "fh", "fields": { "NUM": 1024 } } },
                  "TO_LOW": { "block": { "type": "core.math.number", "id": "tl", "fields": { "NUM": 0 } } },
                  "TO_HIGH": { "block": { "type": "core.math.number", "id": "th", "fields": { "NUM": 4 } } }
                }
              } } }
            }
          ] }
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast());

        Assert.True(result.Success);
        Assert.Equal(["2"], result.Output);
    }

    [Fact]
    public async Task Lists_MakeCodeSet_ZeroBasedAndMutable()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "core.variables.set", "id": "s1",
              "fields": { "VAR": { "id": "varL" } },
              "inputs": { "VALUE": { "block": {
                "type": "lists_create_with", "id": "cw",
                "inputs": {
                  "ADD0": { "block": { "type": "core.text.text", "id": "a1", "fields": { "TEXT": "a" } } },
                  "ADD1": { "block": { "type": "core.text.text", "id": "a2", "fields": { "TEXT": "b" } } },
                  "ADD2": { "block": { "type": "core.text.text", "id": "a3", "fields": { "TEXT": "c" } } }
                }
              } } },
              "next": { "block": {
                "type": "lists_index_set", "id": "st",
                "inputs": {
                  "LIST": { "block": { "type": "core.variables.get", "id": "g0", "fields": { "VAR": { "id": "varL" } } } },
                  "INDEX": { "block": { "type": "core.math.number", "id": "n0", "fields": { "NUM": 1 } } },
                  "VALUE": { "block": { "type": "core.text.text", "id": "t0", "fields": { "TEXT": "x" } } }
                },
                "next": { "block": {
                  "type": "core.text.print", "id": "p1",
                  "inputs": { "TEXT": { "block": { "type": "core.variables.get", "id": "g1", "fields": { "VAR": { "id": "varL" } } } } },
                  "next": { "block": {
                    "type": "core.text.print", "id": "p2",
                    "inputs": { "TEXT": { "block": {
                      "type": "lists_index_get", "id": "gt",
                      "inputs": {
                        "LIST": { "block": { "type": "core.variables.get", "id": "g2", "fields": { "VAR": { "id": "varL" } } } },
                        "INDEX": { "block": { "type": "core.math.number", "id": "n2", "fields": { "NUM": 0 } } }
                      }
                    } } },
                    "next": { "block": {
                      "type": "core.text.print", "id": "p3",
                      "inputs": { "TEXT": { "block": {
                        "type": "lists_length", "id": "ln",
                        "inputs": { "VALUE": { "block": { "type": "core.variables.get", "id": "g3", "fields": { "VAR": { "id": "varL" } } } } }
                      } } },
                      "next": { "block": {
                        "type": "core.text.print", "id": "p4",
                        "inputs": { "TEXT": { "block": {
                          "type": "array_indexof", "id": "io",
                          "inputs": {
                            "LIST": { "block": { "type": "core.variables.get", "id": "g4", "fields": { "VAR": { "id": "varL" } } } },
                            "VALUE": { "block": { "type": "core.text.text", "id": "t4", "fields": { "TEXT": "c" } } }
                          }
                        } } },
                        "next": { "block": {
                          "type": "core.text.print", "id": "p5",
                          "inputs": { "TEXT": { "block": {
                            "type": "lists_repeat", "id": "rp",
                            "inputs": {
                              "ITEM": { "block": { "type": "core.text.text", "id": "t5", "fields": { "TEXT": "z" } } },
                              "NUM": { "block": { "type": "core.math.number", "id": "n5", "fields": { "NUM": 2 } } }
                            }
                          } } }
                        } } }
                      } }
                    } }
                  } }
                } }
              } }
          ] },
          "variables": [ { "id": "varL", "name": "list" } ]
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast());

        Assert.True(result.Success);
        // set по индексу 1 мутирует список; индексы 0-основные; indexof — 0-based
        Assert.Equal(["a,x,c", "a", "3", "2", "z,z"], result.Output);
    }

    [Fact]
    public async Task Lists_ModifyOps_PushPopShiftUnshiftInsertRemoveReverse()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            { "type": "core.variables.set", "id": "s1", "fields": { "VAR": { "id": "varL" } },
              "inputs": { "VALUE": { "block": { "type": "lists_create_with", "id": "cw", "inputs": {
                "ADD0": { "block": { "type": "core.text.text", "id": "a1", "fields": { "TEXT": "a" } } },
                "ADD1": { "block": { "type": "core.text.text", "id": "a2", "fields": { "TEXT": "b" } } },
                "ADD2": { "block": { "type": "core.text.text", "id": "a3", "fields": { "TEXT": "c" } } }
              } } } },
              "next": { "block": { "type": "array_push", "id": "pu",
                "inputs": {
                  "LIST": { "block": { "type": "core.variables.get", "id": "g0", "fields": { "VAR": { "id": "varL" } } } },
                  "VALUE": { "block": { "type": "core.text.text", "id": "t0", "fields": { "TEXT": "d" } } }
                },
                "next": { "block": { "type": "core.text.print", "id": "p1",
                  "inputs": { "TEXT": { "block": { "type": "core.variables.get", "id": "g1", "fields": { "VAR": { "id": "varL" } } } } },
                  "next": { "block": { "type": "core.text.print", "id": "p2",
                    "inputs": { "TEXT": { "block": { "type": "array_pop", "id": "po",
                      "inputs": { "LIST": { "block": { "type": "core.variables.get", "id": "g2", "fields": { "VAR": { "id": "varL" } } } } }
                    } } },
                    "next": { "block": { "type": "core.text.print", "id": "p3",
                      "inputs": { "TEXT": { "block": { "type": "array_shift", "id": "sh",
                        "inputs": { "LIST": { "block": { "type": "core.variables.get", "id": "g3", "fields": { "VAR": { "id": "varL" } } } } }
                      } } },
                      "next": { "block": { "type": "array_unshift_statement", "id": "un",
                        "inputs": {
                          "LIST": { "block": { "type": "core.variables.get", "id": "g4", "fields": { "VAR": { "id": "varL" } } } },
                          "VALUE": { "block": { "type": "core.text.text", "id": "t4", "fields": { "TEXT": "z" } } }
                        },
                        "next": { "block": { "type": "core.text.print", "id": "p4",
                          "inputs": { "TEXT": { "block": { "type": "core.variables.get", "id": "g5", "fields": { "VAR": { "id": "varL" } } } } },
                          "next": { "block": { "type": "array_insertAt", "id": "in",
                            "inputs": {
                              "LIST": { "block": { "type": "core.variables.get", "id": "g6", "fields": { "VAR": { "id": "varL" } } } },
                              "INDEX": { "block": { "type": "core.math.number", "id": "n6", "fields": { "NUM": 1 } } },
                              "VALUE": { "block": { "type": "core.text.text", "id": "t6", "fields": { "TEXT": "x" } } }
                            },
                            "next": { "block": { "type": "core.text.print", "id": "p5",
                              "inputs": { "TEXT": { "block": { "type": "core.variables.get", "id": "g7", "fields": { "VAR": { "id": "varL" } } } } },
                              "next": { "block": { "type": "core.text.print", "id": "p6",
                                "inputs": { "TEXT": { "block": { "type": "array_removeat", "id": "ra",
                                  "inputs": {
                                    "LIST": { "block": { "type": "core.variables.get", "id": "g8", "fields": { "VAR": { "id": "varL" } } } },
                                    "INDEX": { "block": { "type": "core.math.number", "id": "n8", "fields": { "NUM": 2 } } }
                                  }
                                } } },
                                "next": { "block": { "type": "array_reverse", "id": "rv",
                                  "inputs": { "LIST": { "block": { "type": "core.variables.get", "id": "g9", "fields": { "VAR": { "id": "varL" } } } } },
                                  "next": { "block": { "type": "core.text.print", "id": "p7",
                                    "inputs": { "TEXT": { "block": { "type": "core.variables.get", "id": "g10", "fields": { "VAR": { "id": "varL" } } } } },
                                    "next": { "block": { "type": "core.text.print", "id": "p8",
                                      "inputs": { "TEXT": { "block": { "type": "array_pickRandom", "id": "pr",
                                        "inputs": { "LIST": { "block": { "type": "lists_create_with", "id": "cw2", "inputs": {
                                          "ADD0": { "block": { "type": "core.text.text", "id": "a9", "fields": { "TEXT": "q" } } }
                                        } } } }
                                      } } }
                                    } } }
                                  } } }
                                } } }
                              } } }
                            } } }
                          } } }
                        } } }
                      } } }
                    }
          ] },
          "variables": [ { "id": "varL", "name": "list" } ]
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast());

        Assert.True(result.Success);
        Assert.Equal(["a,b,c,d", "d", "a", "z,b,c", "z,x,b,c", "b", "c,x,z", "q"], result.Output);
    }

    [Fact]
    public async Task Functions_TypedArgs_CallOutput_AndMissingArgDefault()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "function_definition", "id": "fd",
              "extraState": { "name": "addOne", "functionid": "f1", "arguments": [ { "id": "a1", "name": "num", "type": "number" } ] },
              "inputs": { "STACK": { "block": {
                "type": "function_return", "id": "fr",
                "inputs": { "RETURN_VALUE": { "block": {
                  "type": "core.math.arithmetic", "id": "ar", "fields": { "OP": "ADD" },
                  "inputs": {
                    "A": { "block": { "type": "argument_reporter_number", "id": "rp", "fields": { "VALUE": "num" } } },
                    "B": { "block": { "type": "core.math.number", "id": "n1", "fields": { "NUM": 1 } } }
                  }
                } } }
              } } }
            },
            {
              "type": "core.text.print", "id": "p1",
              "inputs": { "TEXT": { "block": {
                "type": "function_call_output", "id": "fc",
                "extraState": { "name": "addOne", "functionid": "f1", "arguments": [ { "id": "a1", "name": "num", "type": "number" } ] },
                "inputs": { "a1": { "block": { "type": "core.math.number", "id": "n5", "fields": { "NUM": 5 } } } }
              } } },
              "next": { "block": {
                "type": "core.text.print", "id": "p2",
                "inputs": { "TEXT": { "block": {
                  "type": "function_call_output", "id": "fc2",
                  "extraState": { "name": "addOne", "functionid": "f1", "arguments": [] }
                } } }
              } }
            }
          ] }
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast());

        Assert.True(result.Success);
        // аргумент 5 → 6; вызов без аргументов → дефолт number 0 → 1
        Assert.Equal(["6", "1"], result.Output);
    }

    [Fact]
    public async Task Functions_EarlyReturn_StopsBody()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "function_definition", "id": "fd",
              "extraState": { "name": "grade", "functionid": "f2", "arguments": [ { "id": "a1", "name": "num", "type": "number" } ] },
              "inputs": { "STACK": { "block": {
                "type": "core.logic.if", "id": "if1",
                "extraState": {},
                "inputs": {
                  "IF0": { "block": {
                    "type": "core.logic.compare", "id": "cmp", "fields": { "OP": "GT" },
                    "inputs": {
                      "A": { "block": { "type": "argument_reporter_number", "id": "rp", "fields": { "VALUE": "num" } } },
                      "B": { "block": { "type": "core.math.number", "id": "n2", "fields": { "NUM": 2 } } }
                    }
                  } },
                  "DO0": { "block": {
                    "type": "function_return", "id": "fr1",
                    "inputs": { "RETURN_VALUE": { "block": { "type": "core.text.text", "id": "tb", "fields": { "TEXT": "big" } } } }
                  } }
                },
                "next": { "block": {
                  "type": "function_return", "id": "fr2",
                  "inputs": { "RETURN_VALUE": { "block": { "type": "core.text.text", "id": "ts", "fields": { "TEXT": "small" } } } }
                } }
              } } }
            },
            {
              "type": "core.text.print", "id": "p1",
              "inputs": { "TEXT": { "block": {
                "type": "function_call_output", "id": "fc1",
                "extraState": { "name": "grade", "functionid": "f2", "arguments": [ { "id": "a1", "name": "num", "type": "number" } ] },
                "inputs": { "a1": { "block": { "type": "core.math.number", "id": "n5", "fields": { "NUM": 5 } } } }
              } } },
              "next": { "block": {
                "type": "core.text.print", "id": "p2",
                "inputs": { "TEXT": { "block": {
                  "type": "function_call_output", "id": "fc2",
                  "extraState": { "name": "grade", "functionid": "f2", "arguments": [ { "id": "a1", "name": "num", "type": "number" } ] },
                  "inputs": { "a1": { "block": { "type": "core.math.number", "id": "n1", "fields": { "NUM": 1 } } } }
                } } }
              } }
            }
          ] }
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast());

        Assert.True(result.Success);
        Assert.Equal(["big", "small"], result.Output);
    }

    [Fact]
    public async Task Functions_IfReturnBlock_ReturnsOnlyWhenTrue()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "function_definition", "id": "fd",
              "extraState": { "name": "pick", "functionid": "f3", "arguments": [ { "id": "b1", "name": "flag", "type": "boolean" } ] },
              "inputs": { "STACK": { "block": {
                "type": "core.functions.if_return", "id": "ir",
                "inputs": {
                  "CONDITION": { "block": { "type": "argument_reporter_boolean", "id": "rb", "fields": { "VALUE": "flag" } } },
                  "VALUE": { "block": { "type": "core.text.text", "id": "ty", "fields": { "TEXT": "yes" } } }
                },
                "next": { "block": {
                  "type": "function_return", "id": "fr",
                  "inputs": { "RETURN_VALUE": { "block": { "type": "core.text.text", "id": "tn", "fields": { "TEXT": "no" } } } }
                } }
              } } }
            },
            {
              "type": "core.text.print", "id": "p1",
              "inputs": { "TEXT": { "block": {
                "type": "function_call_output", "id": "fc1",
                "extraState": { "name": "pick", "functionid": "f3", "arguments": [ { "id": "b1", "name": "flag", "type": "boolean" } ] },
                "inputs": { "b1": { "block": { "type": "core.logic.boolean", "id": "bt", "fields": { "BOOL": "TRUE" } } } }
              } } },
              "next": { "block": {
                "type": "core.text.print", "id": "p2",
                "inputs": { "TEXT": { "block": {
                  "type": "function_call_output", "id": "fc2",
                  "extraState": { "name": "pick", "functionid": "f3", "arguments": [ { "id": "b1", "name": "flag", "type": "boolean" } ] },
                  "inputs": { "b1": { "block": { "type": "core.logic.boolean", "id": "bf", "fields": { "BOOL": "FALSE" } } } }
                } } }
              } }
            }
          ] }
        }
        """;

        var result = await PxTestRun.RunAsync(json, PxTestRun.Fast());

        Assert.True(result.Success);
        Assert.Equal(["yes", "no"], result.Output);
    }

    [Fact]
    public async Task LogicOperation_ShortCircuit_And()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "core.text.print", "id": "p1",
              "inputs": { "TEXT": { "block": {
                "type": "core.logic.operation", "id": "and1", "fields": { "OP": "AND" },
                "inputs": {
                  "A": { "block": { "type": "core.logic.boolean", "id": "f", "fields": { "BOOL": "FALSE" } } },
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
              "type": "core.text.print", "id": "p1",
              "inputs": { "TEXT": { "block": {
                "type": "core.logic.ternary", "id": "t1",
                "inputs": {
                  "IF": { "block": { "type": "core.logic.boolean", "id": "f", "fields": { "BOOL": "FALSE" } } },
                  "THEN": { "block": { "type": "test_probe", "id": "probe1" } },
                  "ELSE": { "block": { "type": "core.math.number", "id": "seven", "fields": { "NUM": 7 } } }
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
              "type": "core.loops.while_until", "id": "w",
              "fields": { "MODE": "WHILE" },
              "inputs": {
                "BOOL": { "block": { "type": "core.logic.boolean", "id": "t", "fields": { "BOOL": "TRUE" } } }
              }
            }
          ] }
        }
        """;

        var result = await PxTestRun.RunAsync(json, new PxRunOptions { StepLimit = 100, YieldEvery = 0 });

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("step limit", result.ErrorMessage);
        Assert.Equal("w", result.ErrorBlockId);
    }

    [Fact]
    public async Task Cancellation_ReturnsCanceled()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            {
              "type": "core.loops.while_until", "id": "w",
              "fields": { "MODE": "WHILE" },
              "inputs": {
                "BOOL": { "block": { "type": "core.logic.boolean", "id": "t", "fields": { "BOOL": "TRUE" } } }
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
              "type": "core.loops.for_each", "id": "fe",
              "fields": { "VAR": { "id": "varI" } },
              "inputs": {
                "LIST": { "block": { "type": "core.math.number", "id": "n", "fields": { "NUM": 1 } } }
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
            { "type": "core.text.print", "id": "print1",
              "inputs": { "TEXT": { "block": { "type": "core.text.text", "id": "t", "fields": { "TEXT": "hi" } } } } }
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
