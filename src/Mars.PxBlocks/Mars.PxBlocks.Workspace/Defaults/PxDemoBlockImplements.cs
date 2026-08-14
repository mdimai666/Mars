using Mars.PxBlocks.Runtime.Execution;
using Mars.PxBlocks.Runtime.Values;

namespace Mars.PxBlocks.Workspace.Defaults;

/// <summary>
/// Исполнение демо-блоков (PxDefaultBlocks): значения — как есть, приёмники —
/// печатают входящее значение, «создать объект» собирает PxObjectValue из пар
/// поле→значение мутатора px_object_builder.
/// </summary>
public sealed class PxDemoNumberImplement : IPxExpressionImplement
{
    public string TypeId => "px_demo_number";

    public ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult<PxValue>(new PxNumberValue(call.FieldNumber("NUM")));
}

public sealed class PxDemoStringImplement : IPxExpressionImplement
{
    public string TypeId => "px_demo_string";

    public ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult<PxValue>(new PxStringValue(call.FieldText("TEXT")));
}

public sealed class PxDemoAnyImplement : IPxExpressionImplement
{
    public string TypeId => "px_demo_any";

    public ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult<PxValue>(new PxNumberValue(42));
}

public sealed class PxDemoObjectImplement : IPxExpressionImplement
{
    public string TypeId => "px_demo_object";

    public ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult<PxValue>(new PxObjectValue());
}

/// <summary>Общий приёмник: печатает входящее значение.</summary>
public abstract class PxDemoTakeImplement(string typeId) : IPxStatementImplement
{
    public string TypeId { get; } = typeId;

    public Task ExecuteAsync(PxContext context, PxCall call)
    {
        context.Print(call.Input("VAL").ToText());
        return Task.CompletedTask;
    }
}

public sealed class PxDemoTakeNumberImplement : PxDemoTakeImplement
{
    public PxDemoTakeNumberImplement() : base("px_demo_take_number") { }
}

public sealed class PxDemoTakeAnyImplement : PxDemoTakeImplement
{
    public PxDemoTakeAnyImplement() : base("px_demo_take_any") { }
}

public sealed class PxDemoTakeObjectImplement : PxDemoTakeImplement
{
    public PxDemoTakeObjectImplement() : base("px_demo_take_object") { }
}

/// <summary>«Создать объект»: пары поле→значение мутатора (входы px_obj_value_N, поля px_obj_key_N).</summary>
public sealed class PxDemoCreateObjectImplement : IPxExpressionImplement
{
    private const string ValuePrefix = "px_obj_value_";
    private const string KeyPrefix = "px_obj_key_";

    public string TypeId => "px_create_object";

    public ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
    {
        var members = new Dictionary<string, PxValue>(StringComparer.Ordinal);
        foreach (var name in call.InputOrder)
        {
            if (!name.StartsWith(ValuePrefix, StringComparison.Ordinal))
                continue;

            var index = name[ValuePrefix.Length..];
            var key = call.FieldText($"{KeyPrefix}{index}", $"поле{index}");
            members[key] = call.Inputs[name];
        }

        return ValueTask.FromResult<PxValue>(new PxObjectValue(members));
    }
}
