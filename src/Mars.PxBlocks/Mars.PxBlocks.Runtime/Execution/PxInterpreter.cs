using Mars.PxBlocks.Runtime.Ast;
using Mars.PxBlocks.Runtime.Values;

namespace Mars.PxBlocks.Runtime.Execution;

/// <summary>
/// Tree-walking интерпретатор PxBlocks (решение «вариант C»): control flow —
/// if/циклы/break-continue/процедуры/переменные/короткое замыкание — живёт в ядре
/// и не плагинится; блоки-листья исполняются имплементациями из локатора.
/// </summary>
public sealed class PxInterpreter
{
    private readonly PxBlockImplementsLocator _implements;

    public PxInterpreter(PxBlockImplementsLocator? implements = null)
        => _implements = implements ?? CreateDefaultImplements();

    public PxBlockImplementsLocator Implements => _implements;

    /// <summary>Локатор по умолчанию: стандартные листья этой сборки (математика, логика, текст…).</summary>
    public static PxBlockImplementsLocator CreateDefaultImplements()
    {
        var locator = new PxBlockImplementsLocator();
        locator.RegisterAssembly(typeof(PxInterpreter).Assembly);
        return locator;
    }

    public async Task<PxExecutionResult> RunAsync(
        PxProgram program,
        PxRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new PxRunOptions();
        var context = new PxContext(program, options, _implements, options.State, cancellationToken);

        try
        {
            if (options.EventNames != null)
            {
                // Режим «только переданные события»: фазы в порядке списка имён —
                // сначала ВСЕ события с первым именем (в порядке workspace), потом
                // со вторым и т.д. При ["start", "loop"] Loop гарантированно после Start.
                foreach (var eventName in options.EventNames.Distinct())
                {
                    foreach (var statement in program.TopLevel)
                    {
                        if (statement is PxEventBlock ev && ev.EventName == eventName)
                            await ExecuteChainAsync(statement, context.Global, context);
                    }
                }
            }
            else
            {
                // Семантика Arduino setup()/loop(): обычные стеки и события Start —
                // в порядке workspace, события Loop — после всех (и повторяются).
                var deferredLoops = new List<PxStatement>();
                foreach (var statement in program.TopLevel)
                {
                    if (statement is PxEventBlock { EventName: PxEvents.Loop })
                    {
                        deferredLoops.Add(statement);
                        continue;
                    }

                    await ExecuteChainAsync(statement, context.Global, context);
                }

                foreach (var loop in deferredLoops)
                    await ExecuteChainAsync(loop, context.Global, context);
            }

            return Result(context, success: true);
        }
        catch (PxReturnSignal)
        {
            return Result(context, success: true); // return вне функции завершает программу
        }
        catch (PxFlowSignal)
        {
            return Result(context, success: true); // break/continue вне цикла — игнорируем
        }
        catch (OperationCanceledException)
        {
            return Result(context, success: false, canceled: true);
        }
        catch (PxRuntimeException exception)
        {
            return Result(context, success: false, exception.Message, exception.BlockId);
        }
    }

    /// <summary>Стек операторов: каждый блок — событие Entered/Exited (материал для подсветки).</summary>
    private static async Task ExecuteChainAsync(PxStatement? node, PxScope scope, PxContext context)
    {
        while (node != null)
        {
            await context.StepAsync(node.BlockId);
            context.Fire(new PxExecutionEvent(PxExecutionEventKind.BlockEntered, node.BlockId, null));
            await ExecuteOneAsync(node, scope, context);
            context.Fire(new PxExecutionEvent(PxExecutionEventKind.BlockExited, node.BlockId, null));
            node = node.Next;
        }
    }

    private static async Task ExecuteOneAsync(PxStatement node, PxScope scope, PxContext context)
    {
        switch (node)
        {
            case PxIfStatement ifStatement:
            {
                foreach (var branch in ifStatement.Branches)
                {
                    if ((await EvaluateAsync(branch.Condition, scope, context)).IsTruthy())
                    {
                        await ExecuteChainAsync(branch.Body, scope, context);
                        return;
                    }
                }

                if (ifStatement.ElseBody != null)
                    await ExecuteChainAsync(ifStatement.ElseBody, scope, context);
                return;
            }

            case PxRepeatStatement repeat:
            {
                var times = (await EvaluateAsync(repeat.Times, scope, context)).ToNumber();
                var repeats = double.IsNaN(times) || times < 0 ? 0 : (long)Math.Floor(times);
                for (long i = 0; i < repeats; i++)
                {
                    await context.StepAsync(repeat.BlockId);
                    if (await RunLoopBodyAsync(repeat.Body, scope, context))
                        return;
                }
                return;
            }

            case PxWhileUntilStatement whileUntil:
            {
                while (true)
                {
                    await context.StepAsync(whileUntil.BlockId);
                    var condition = (await EvaluateAsync(whileUntil.Condition, scope, context)).IsTruthy();
                    if (whileUntil.Mode == PxWhileMode.Until)
                        condition = !condition;
                    if (!condition)
                        return;
                    if (await RunLoopBodyAsync(whileUntil.Body, scope, context))
                        return;
                }
            }

            case PxForStatement forLoop:
            {
                var from = (await EvaluateAsync(forLoop.From, scope, context)).ToNumber();
                var to = (await EvaluateAsync(forLoop.To, scope, context)).ToNumber();
                var by = (await EvaluateAsync(forLoop.By, scope, context)).ToNumber();
                if (by == 0)
                    throw new PxRuntimeException("Шаг цикла не может быть нулём", forLoop.BlockId);

                for (var value = from; by > 0 ? value <= to : value >= to; value += by)
                {
                    await context.StepAsync(forLoop.BlockId);
                    scope.Set(forLoop.VariableId, new PxNumberValue(value));
                    if (await RunLoopBodyAsync(forLoop.Body, scope, context))
                        return;
                }
                return;
            }

            case PxForEachStatement forEach:
            {
                var listValue = await EvaluateAsync(forEach.List, scope, context);
                if (listValue is not PxListValue list)
                    throw new PxRuntimeException("«Для каждого» требует список", forEach.BlockId);

                foreach (var item in list.Items)
                {
                    await context.StepAsync(forEach.BlockId);
                    scope.Set(forEach.VariableId, item);
                    if (await RunLoopBodyAsync(forEach.Body, scope, context))
                        return;
                }
                return;
            }

            case PxFlowStatement flow:
                throw new PxFlowSignal(flow.Kind);

            case PxEventBlock eventBlock when eventBlock.EventName == PxEvents.Loop:
            {
                // Аналог loop() в Arduino: тело повторяется, пока не остановят
                // (CancellationToken/лимит шагов) или не выйдут через break.
                while (true)
                {
                    await context.StepAsync(eventBlock.BlockId);
                    if (await RunLoopBodyAsync(eventBlock.Body, scope, context))
                        return;
                }
            }

            case PxEventBlock eventBlock:
                await ExecuteChainAsync(eventBlock.Body, scope, context);
                return;

            case PxProcedureDef:
                return; // определения собраны парсером в PxProgram.Procedures

            case PxProcedureCallStatement call:
                await CallProcedureAsync(call.Name, call.Args, call.BlockId, scope, context);
                return;

            case PxVariableSet set:
                scope.Set(set.VariableId, await EvaluateAsync(set.Value, scope, context));
                return;

            case PxIfReturnStatement ifReturn:
            {
                if ((await EvaluateAsync(ifReturn.Condition, scope, context)).IsTruthy())
                {
                    var value = ifReturn.Value == null
                        ? null
                        : await EvaluateAsync(ifReturn.Value, scope, context);
                    throw new PxReturnSignal(value);
                }
                return;
            }

            case PxLeafStatement leaf:
                await ExecuteLeafAsync(leaf, scope, context);
                return;

            default:
                throw new PxRuntimeException($"Неизвестный оператор '{node.TypeId}'", node.BlockId);
        }
    }

    /// <summary>Тело цикла: true — выходим (break); continue просто начинает следующую итерацию.</summary>
    private static async Task<bool> RunLoopBodyAsync(PxStatement? body, PxScope scope, PxContext context)
    {
        try
        {
            await ExecuteChainAsync(body, scope, context);
            return false;
        }
        catch (PxFlowSignal signal)
        {
            return signal.Kind == PxFlowKind.Break;
        }
    }

    private static async ValueTask<PxValue> CallProcedureAsync(
        string name,
        IReadOnlyList<PxExpression> args,
        string blockId,
        PxScope callerScope,
        PxContext context)
    {
        if (!context.Procedures.TryGetValue(name, out var definition))
            throw new PxRuntimeException($"Функция '{name}' не определена", blockId);

        // Рамка функции: параметры локальны, остальное видно до глобального скоупа.
        var scope = new PxScope(context.Global);
        for (var i = 0; i < definition.Params.Count; i++)
        {
            var value = i < args.Count
                ? await EvaluateAsync(args[i], callerScope, context)
                : PxNumberValue.Zero;
            scope.Define(definition.Params[i].Id, value);
        }

        try
        {
            await ExecuteChainAsync(definition.Body, scope, context);
            if (definition.Return != null)
                return await EvaluateAsync(definition.Return, scope, context);
            return PxNullValue.Instance;
        }
        catch (PxReturnSignal signal)
        {
            return signal.Value ?? PxNullValue.Instance;
        }
    }

    private static async ValueTask<PxValue> EvaluateAsync(PxExpression expression, PxScope scope, PxContext context)
    {
        switch (expression)
        {
            case PxVariableGet get:
                return scope.Get(get.VariableId);

            case PxNullLiteral:
                return PxNullValue.Instance;

            case PxNumberLiteral number:
                return new PxNumberValue(number.Number);

            case PxLogicOperation operation:
            {
                var left = await EvaluateAsync(operation.Left, scope, context);
                var shortCircuits = operation.Op == PxLogicOp.And ? !left.IsTruthy() : left.IsTruthy();
                if (shortCircuits)
                    return new PxBooleanValue(operation.Op == PxLogicOp.Or);

                var right = await EvaluateAsync(operation.Right, scope, context);
                return new PxBooleanValue(right.IsTruthy());
            }

            case PxLogicTernary ternary:
            {
                var condition = await EvaluateAsync(ternary.Condition, scope, context);
                return await EvaluateAsync(condition.IsTruthy() ? ternary.Then : ternary.Else, scope, context);
            }

            case PxProcedureCallExpression call:
                return await CallProcedureAsync(call.Name, call.Args, call.BlockId, scope, context);

            case PxLeafExpression leaf:
                return await EvaluateLeafAsync(leaf, scope, context);

            default:
                throw new PxRuntimeException($"Неизвестное выражение '{expression.TypeId}'", expression.BlockId);
        }
    }

    private static async ValueTask<PxValue> EvaluateLeafAsync(PxLeafExpression leaf, PxScope scope, PxContext context)
    {
        if (context.Implement(leaf.TypeId) is not IPxExpressionImplement implement)
            throw new PxRuntimeException($"Для блока '{leaf.TypeId}' не зарегистрирована реализация", leaf.BlockId);

        var (inputs, order) = await EvaluateInputsAsync(leaf.Inputs, scope, context);
        return await implement.EvaluateAsync(context, new PxCall(leaf.BlockId, inputs, leaf.Fields)
        {
            InputOrder = order,
            ExtraState = leaf.ExtraState
        });
    }

    private static async Task ExecuteLeafAsync(PxLeafStatement leaf, PxScope scope, PxContext context)
    {
        if (context.Implement(leaf.TypeId) is not IPxStatementImplement implement)
            throw new PxRuntimeException($"Для блока '{leaf.TypeId}' не зарегистрирована реализация", leaf.BlockId);

        var (inputs, order) = await EvaluateInputsAsync(leaf.Inputs, scope, context);
        await implement.ExecuteAsync(context, new PxCall(leaf.BlockId, inputs, leaf.Fields)
        {
            InputOrder = order,
            ExtraState = leaf.ExtraState
        });
    }

    private static async Task<(Dictionary<string, PxValue> Inputs, List<string> Order)> EvaluateInputsAsync(
        List<(string Name, PxExpression Expr)> inputs, PxScope scope, PxContext context)
    {
        var values = new Dictionary<string, PxValue>(StringComparer.Ordinal);
        var order = new List<string>(inputs.Count);
        foreach (var (name, input) in inputs)
        {
            values[name] = await EvaluateAsync(input, scope, context);
            order.Add(name);
        }

        return (values, order);
    }

    private static PxExecutionResult Result(
        PxContext context,
        bool success,
        string? errorMessage = null,
        string? errorBlockId = null,
        bool canceled = false)
        => new()
        {
            Success = success,
            ErrorMessage = errorMessage,
            ErrorBlockId = errorBlockId,
            Canceled = canceled,
            Steps = context.Steps,
            Output = context.OutputLines
        };
}
