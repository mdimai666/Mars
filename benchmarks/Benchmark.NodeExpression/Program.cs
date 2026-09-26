using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DynamicExpresso;
using Mars.Nodes.Abstractions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes.Common;
using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Expressions;
using Microsoft.Extensions.DependencyInjection;
using NCalcExpression = NCalc.Expression;

if (args.Contains("--semantic"))
{
    SemanticComparison.Run();
    return;
}

BenchmarkRunner.Run<NodeExpressionBenchmark>();

/// <summary>
/// Стоимость чтения поля ноды на каждом сообщении:
/// прямое чтение против интерпретации выражения "msg.Payload" (DynamicExpresso)
/// против hot path (спец-случай в резолвере) против кэша скомпилированной Lambda.
/// Все ветки имитируют исполнение ноды с одним полем Payload (VarType=string) на одном сообщении.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class NodeExpressionBenchmark
{
    const string Expression = "msg.Payload";
    const string VarType = "string";

    static readonly ExecuteAction Callback = static (_, _) => { };

    RuntimeNodeScopeMock _rns = null!;
    NodeMsg _msg = null!;
    ExecutionParameters _parameters = null!;
    ExpressionScope _scope;
    InjectNode _node = null!;

    InjectNodeImpl _productionImpl = null!;
    InjectNodeImpl _arithmeticImpl = null!;
    InjectNodeImpl _complexImpl = null!;
    NodeMsg _arithmeticMsg = null!;
    NodeMsg _complexMsg = null!;

    Interpreter _bindInterpreter = null!;
    Interpreter _parseInterpreter = null!;

    Interpreter _cachedInterpreter = null!;
    Lambda _cachedLambda = null!;

    NCalcExpression _ncalcCached = null!;

    Interpreter _parameterizedInterpreter = null!;
    Lambda _parameterizedLambda = null!;

    [GlobalSetup]
    public void Setup()
    {
        _rns = new RuntimeNodeScopeMock(new ServiceCollection().AddSingleton<ExpressionRunnerPool>().BuildServiceProvider());

        _msg = new NodeMsg { Payload = "hello" };
        _msg.Set("topic", "sensor/1");
        _msg.Set("count", 42);
        _msg.Set("ts", 1758800000000L);

        _parameters = new ExecutionParameters(Guid.NewGuid(), Guid.NewGuid(), 0, CancellationToken.None, 0);
        _scope = new ExpressionScope(_rns, _msg);

        _node = new InjectNode
        {
            Fields =
            [
                new InjectNodeField
                {
                    Key = InjectNode.PayloadKey,
                    VarType = VarType,
                    Value = Expression,
                    ValueKind = InputValueKind.Expression
                }
            ]
        };

        _productionImpl = new InjectNodeImpl(_node, _rns);

        var arithmeticNode = new InjectNode
        {
            Fields =
            [
                new InjectNodeField
                {
                    Key = InjectNode.PayloadKey,
                    VarType = "int",
                    Value = "msg.count * 2 + 1",
                    ValueKind = InputValueKind.Expression
                }
            ]
        };
        _arithmeticMsg = new NodeMsg { Payload = 0 };
        _arithmeticMsg.Set("topic", "sensor/1");
        _arithmeticMsg.Set("count", 42);
        _arithmeticMsg.Set("ts", 1758800000000L);
        _arithmeticImpl = new InjectNodeImpl(arithmeticNode, _rns);

        var complexNode = new InjectNode
        {
            Fields =
            [
                new InjectNodeField
                {
                    Key = "result", // не Payload — чтобы не мутировать строку между итерациями
                    VarType = "int",
                    Value = "msg.Payload.Count() + 1",
                    ValueKind = InputValueKind.Expression
                }
            ]
        };
        _complexMsg = new NodeMsg { Payload = "hello" };
        _complexMsg.Set("topic", "sensor/1");
        _complexMsg.Set("count", 42);
        _complexImpl = new InjectNodeImpl(complexNode, _rns);

        _bindInterpreter = InputValueResolver.CreateInterpreter(_rns, _msg);
        _parseInterpreter = InputValueResolver.CreateInterpreter(_rns, _msg);

        // Ветка 4: Parse один раз, на сообщение только SetVariable + Invoke
        _cachedInterpreter = InputValueResolver.CreateInterpreter(_rns);
        _cachedInterpreter.SetVariable("msg", new DynamicNodeMsgWrapper(_msg));
        _cachedLambda = _cachedInterpreter.Parse(Expression, typeof(object));

        _ncalcCached = new NCalcExpression("[msg_Payload]");
        _ncalcCached.Parameters["msg_Payload"] = _msg.Payload!;

        // Ветка 5: Lambda с объявленными параметрами — без SetVariable и без DynamicNodeMsgWrapper на Invoke
        _parameterizedInterpreter = InputValueResolver.CreateInterpreter(_rns);
        _parameterizedLambda = _parameterizedInterpreter.Parse(
            "msg_Payload", typeof(object), new Parameter("msg_Payload", typeof(object)));
    }

    // === Ветки исполнения ноды ===

    /// <summary>Ветка 1: impl без интерпретатора — прямое чтение msg.Payload.</summary>
    [Benchmark(Baseline = true)]
    public object? DirectRead()
    {
        object? value = _msg.Payload;
        value = value?.ToString() ?? ""; // конверсия VarType=string как в ConvertToVarType
        _msg.Payload = value;
        Callback(_msg);
        return value;
    }

    /// <summary>Ветка 2: текущий продакшен-путь — InjectNodeImpl + InputValueResolver как есть.</summary>
    [Benchmark]
    public object? ExpressionCurrent()
    {
        _productionImpl.Execute(_msg, Callback, _parameters).GetAwaiter().GetResult();
        return _msg.Payload;
    }

    /// <summary>Ветка 2b: прод-путь с простой арифметикой (после фазы 3 — NCalc).</summary>
    [Benchmark]
    public object? ExpressionCurrentArithmetic()
    {
        _arithmeticImpl.Execute(_arithmeticMsg, Callback, _parameters).GetAwaiter().GetResult();
        return _arithmeticMsg.Payload;
    }

    /// <summary>Ветка 2c: прод-путь со сложным C#-выражением (после фазы 4 — DE Lambda-кэш).</summary>
    [Benchmark]
    public object? ExpressionCurrentComplex()
    {
        _complexImpl.Execute(_complexMsg, Callback, _parameters).GetAwaiter().GetResult();
        return _complexMsg.Get("result");
    }

    /// <summary>Ветка 3: прототип hot path — точное совпадение с "msg.Payload" минует интерпретатор.</summary>
    [Benchmark]
    public object? ExpressionHotPath()
    {
        var field = _node.Fields[0];

        object? value;
        if (field.ValueKind == InputValueKind.Expression && field.Value is Expression)
        {
            value = _msg.Payload; // без CreateInterpreter, BindRootPaths и Eval
        }
        else
        {
            var interpreter = InputValueResolver.CreateInterpreter(_rns, _msg);
            value = InputValueResolver.Resolve(field.ValueKind, field.Value, field.VarType, interpreter, _scope, _node, "bench");
        }

        value = value?.ToString() ?? "";
        _msg.Payload = value;
        Callback(_msg);
        return value;
    }

    /// <summary>Ветка 4: стандартная оптимизация DynamicExpresso — Parse один раз, кэш Lambda, на сообщение SetVariable + Invoke.</summary>
    [Benchmark]
    public object? ExpressionCachedLambda()
    {
        _cachedInterpreter.SetVariable("msg", new DynamicNodeMsgWrapper(_msg));
        var raw = _cachedLambda.Invoke();
        var value = raw?.ToString() ?? "";
        _msg.Payload = value;
        Callback(_msg);
        return value;
    }

    // === NCalc (v7.2.0): эксперимент — замена/дополнение для простых выражений ===

    /// <summary>NCalc A: new Expression на сообщение, значение подставляется параметром (аналог BindRootPaths), reliance на внутренний parse-кэш NCalc.</summary>
    [Benchmark]
    public object? NCalcBracketParam()
    {
        var expr = new NCalcExpression("[msg_Payload]");
        expr.Parameters["msg_Payload"] = _msg.Payload!;
        return expr.Evaluate()?.ToString() ?? "";
    }

    /// <summary>NCalc B: подход из git-истории (SwitchNodeImpl @35933d6b) — имя с точкой в скобках + EvaluateParameter.</summary>
    [Benchmark]
    public object? NCalcEvaluateParameterEvent()
    {
        var expr = new NCalcExpression("[msg.Payload]");
        var payload = _msg.Payload;
        expr.EvaluateParameter += (name, args) =>
        {
            if (name == "msg.Payload")
                args.Result = payload!;
        };
        return expr.Evaluate()?.ToString() ?? "";
    }

    /// <summary>NCalc C: кэш объекта Expression, на сообщение только Parameters + Evaluate.</summary>
    [Benchmark]
    public object? NCalcCachedExpression()
    {
        _ncalcCached.Parameters["msg_Payload"] = _msg.Payload!;
        return _ncalcCached.Evaluate()?.ToString() ?? "";
    }

    // === DynamicExpresso: Lambda с объявленными параметрами ===

    /// <summary>Ветка 5a: кэш Lambda, значение подаётся параметром на Invoke (нижняя граница, без резолвинга пути).</summary>
    [Benchmark]
    public object? DynExpressoParamLambda()
    {
        var raw = _parameterizedLambda.Invoke(new Parameter("msg_Payload", _msg.Payload!));
        return raw?.ToString() ?? "";
    }

    /// <summary>Ветка 5b: то же + честный резолвинг пути msg.Payload через DynamicNodeMsgWrapper.GetValueByPath (как в TryResolveRootPath).</summary>
    [Benchmark]
    public object? DynExpressoParamLambdaWithPathResolve()
    {
        var resolved = new DynamicNodeMsgWrapper(_msg).GetValueByPath("Payload");
        var raw = _parameterizedLambda.Invoke(new Parameter("msg_Payload", resolved!));
        return raw?.ToString() ?? "";
    }

    // === Декомпозиция текущего пути ===

    [Benchmark]
    public Interpreter CreateInterpreter() => InputValueResolver.CreateInterpreter(_rns, _msg);

    [Benchmark]
    public string BindRootPaths() => InputValueResolver.BindRootPaths(Expression, _bindInterpreter, _scope);

    [Benchmark]
    public Lambda ParseExpression() => _parseInterpreter.Parse(Expression, typeof(object));

    [Benchmark]
    public object? EvalRawExpression() => _parseInterpreter.Eval(Expression);
}
