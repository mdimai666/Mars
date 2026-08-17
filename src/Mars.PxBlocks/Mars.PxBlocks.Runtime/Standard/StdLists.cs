using Mars.PxBlocks.Runtime.Execution;
using Mars.PxBlocks.Runtime.Values;

namespace Mars.PxBlocks.Runtime.Standard;

/// <summary>Массивы набора MakeCode (Этап 14B): индексы 0-основные, списки мутабельны
/// по ссылке (JS-семантика). create_empty/create_with/repeat/length исполняют и
/// встроенные блоки Blockly (toolbox), и серверные определения — typeId общие.</summary>
internal sealed class StdListsCreateEmpty : PxExpressionImplement
{
    public StdListsCreateEmpty() : base("lists_create_empty") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult<PxValue>(new PxListValue());
}

internal sealed class StdListsCreateWith : PxExpressionImplement
{
    public StdListsCreateWith() : base("lists_create_with") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult<PxValue>(new PxListValue(
            call.InputOrder
                .Where(name => name.StartsWith("ADD", StringComparison.Ordinal))
                .Select(call.Input)
                .ToList()));
}

internal sealed class StdListsRepeat : PxExpressionImplement
{
    public StdListsRepeat() : base("lists_repeat") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
    {
        var item = call.Input("ITEM");
        var count = Math.Max(0, (int)call.Input("NUM").ToNumber());
        return ValueTask.FromResult<PxValue>(new PxListValue(Enumerable.Repeat(item, count).ToList()));
    }
}

internal sealed class StdListsLength : PxExpressionImplement
{
    public StdListsLength() : base("lists_length") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
    {
        var value = call.Input("VALUE");
        var length = value is PxListValue list ? list.Items.Count : value.ToText().Length;
        return ValueTask.FromResult<PxValue>(new PxNumberValue(length));
    }
}

internal sealed class StdListsIndexOf : PxExpressionImplement
{
    public StdListsIndexOf() : base("array_indexof") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
    {
        var list = Lists.List(call);
        var value = call.Input("VALUE");

        for (var index = 0; index < list.Items.Count; index++)
        {
            if (list.Items[index].Equals(value))
                return ValueTask.FromResult<PxValue>(new PxNumberValue(index));
        }

        return ValueTask.FromResult<PxValue>(new PxNumberValue(-1));
    }
}

internal sealed class StdListsGet : PxExpressionImplement
{
    public StdListsGet() : base("lists_index_get") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
    {
        var list = Lists.List(call);
        var index = (int)call.Input("INDEX").ToNumber();

        var result = index >= 0 && index < list.Items.Count ? list.Items[index] : PxNullValue.Instance;
        return ValueTask.FromResult<PxValue>(result);
    }
}

internal sealed class StdListsSet : PxStatementImplement
{
    public StdListsSet() : base("lists_index_set") { }

    public override Task ExecuteAsync(PxContext context, PxCall call)
    {
        var list = Lists.List(call);
        var index = Math.Max(0, (int)call.Input("INDEX").ToNumber());
        list.SetAt(index, call.Input("VALUE"));
        return Task.CompletedTask;
    }
}

internal sealed class StdArrayPush : PxStatementImplement
{
    public StdArrayPush() : base("array_push") { }

    public override Task ExecuteAsync(PxContext context, PxCall call)
    {
        Lists.List(call).Append(call.Input("VALUE"));
        return Task.CompletedTask;
    }
}

internal sealed class StdArrayPop : PxExpressionImplement
{
    public StdArrayPop() : base("array_pop") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult(Lists.List(call).RemoveLast());
}

internal sealed class StdArrayPopStatement : PxStatementImplement
{
    public StdArrayPopStatement() : base("array_pop_statement") { }

    public override Task ExecuteAsync(PxContext context, PxCall call)
    {
        Lists.List(call).RemoveLast();
        return Task.CompletedTask;
    }
}

internal sealed class StdArrayShift : PxExpressionImplement
{
    public StdArrayShift() : base("array_shift") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult(Lists.List(call).RemoveFirst());
}

internal sealed class StdArrayShiftStatement : PxStatementImplement
{
    public StdArrayShiftStatement() : base("array_shift_statement") { }

    public override Task ExecuteAsync(PxContext context, PxCall call)
    {
        Lists.List(call).RemoveFirst();
        return Task.CompletedTask;
    }
}

internal sealed class StdArrayUnshift : PxExpressionImplement
{
    public StdArrayUnshift() : base("array_unshift") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult<PxValue>(new PxNumberValue(
            Lists.List(call).AddFirst(call.Input("VALUE"))));
}

internal sealed class StdArrayUnshiftStatement : PxStatementImplement
{
    public StdArrayUnshiftStatement() : base("array_unshift_statement") { }

    public override Task ExecuteAsync(PxContext context, PxCall call)
    {
        Lists.List(call).InsertFirst(call.Input("VALUE"));
        return Task.CompletedTask;
    }
}

internal sealed class StdArrayRemoveAt : PxExpressionImplement
{
    public StdArrayRemoveAt() : base("array_removeat") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult(Lists.List(call).RemoveAt((int)call.Input("INDEX").ToNumber()));
}

internal sealed class StdArrayRemoveAtStatement : PxStatementImplement
{
    public StdArrayRemoveAtStatement() : base("array_removeat_statement") { }

    public override Task ExecuteAsync(PxContext context, PxCall call)
    {
        Lists.List(call).RemoveAt((int)call.Input("INDEX").ToNumber());
        return Task.CompletedTask;
    }
}

internal sealed class StdArrayInsertAt : PxStatementImplement
{
    public StdArrayInsertAt() : base("array_insertAt") { }

    public override Task ExecuteAsync(PxContext context, PxCall call)
    {
        Lists.List(call).InsertAt((int)call.Input("INDEX").ToNumber(), call.Input("VALUE"));
        return Task.CompletedTask;
    }
}

internal sealed class StdArrayPickRandom : PxExpressionImplement
{
    public StdArrayPickRandom() : base("array_pickRandom") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
    {
        var list = Lists.List(call);
        var result = list.Items.Count == 0
            ? PxNullValue.Instance
            : list.Items[context.Random.Next(list.Items.Count)];
        return ValueTask.FromResult<PxValue>(result);
    }
}

internal sealed class StdArrayReverse : PxStatementImplement
{
    public StdArrayReverse() : base("array_reverse") { }

    public override Task ExecuteAsync(PxContext context, PxCall call)
    {
        Lists.List(call).Reverse();
        return Task.CompletedTask;
    }
}

file static class Lists
{
    public static PxListValue List(PxCall call) =>
        call.Input("LIST") as PxListValue
        ?? throw new PxRuntimeException("the list input is not an array", call.BlockId);
}
