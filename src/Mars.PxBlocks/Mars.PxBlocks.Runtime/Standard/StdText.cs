using System.Globalization;
using System.Text;
using Mars.PxBlocks.Runtime.Execution;
using Mars.PxBlocks.Runtime.Values;

namespace Mars.PxBlocks.Runtime.Standard;

internal sealed class StdTextJoin : PxExpressionImplement
{
    public StdTextJoin() : base("core.text.join") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
    {
        var builder = new StringBuilder();
        foreach (var name in call.InputOrder)
            builder.Append(call.Input(name).ToText());

        return ValueTask.FromResult<PxValue>(new PxStringValue(builder.ToString()));
    }
}

/// <summary>core.text.append: дописать к переменной — использует переменные контекста.</summary>
internal sealed class StdTextAppend : PxStatementImplement
{
    public StdTextAppend() : base("core.text.append") { }

    public override Task ExecuteAsync(PxContext context, PxCall call)
    {
        var variableId = call.FieldVariable("VAR")
            ?? throw new PxRuntimeException("«Добавить текст» без переменной", call.BlockId);

        var current = context.GetVariable(variableId);
        context.SetVariable(variableId, new PxStringValue(current.ToText() + call.Input("TEXT").ToText()));
        return Task.CompletedTask;
    }
}

internal sealed class StdTextLength : PxExpressionImplement
{
    public StdTextLength() : base("core.text.length") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult<PxValue>(new PxNumberValue(call.Input("VALUE").ToText().Length));
}

internal sealed class StdTextIsEmpty : PxExpressionImplement
{
    public StdTextIsEmpty() : base("core.text.is_empty") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult<PxValue>(new PxBooleanValue(string.IsNullOrEmpty(call.Input("VALUE").ToText())));
}

/// <summary>core.text.index_of: результат 1-основный; 0 — не найдено (семантика Blockly).</summary>
internal sealed class StdTextIndexOf : PxExpressionImplement
{
    public StdTextIndexOf() : base("core.text.index_of") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
    {
        var text = call.Input("VALUE").ToText();
        var find = call.Input("FIND").ToText();

        var index = call.FieldText("END") == "LAST"
            ? text.LastIndexOf(find, StringComparison.Ordinal)
            : text.IndexOf(find, StringComparison.Ordinal);

        return ValueTask.FromResult<PxValue>(new PxNumberValue(index + 1));
    }
}

internal sealed class StdTextCharAt : PxExpressionImplement
{
    public StdTextCharAt() : base("core.text.char_at") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
    {
        var text = call.Input("VALUE").ToText();
        if (text.Length == 0)
            return ValueTask.FromResult<PxValue>(PxStringValue.Empty);

        var index = call.FieldText("WHERE") switch
        {
            "FROM_START" => (int)call.Input("AT").ToNumber() - 1,
            "FROM_END" => text.Length - (int)call.Input("AT").ToNumber(),
            "FIRST" => 0,
            "LAST" => text.Length - 1,
            "RANDOM" => context.Random.Next(text.Length),
            _ => 0
        };

        if (index < 0 || index >= text.Length)
            return ValueTask.FromResult<PxValue>(PxStringValue.Empty);

        return ValueTask.FromResult<PxValue>(new PxStringValue(text[index].ToString()));
    }
}

internal sealed class StdTextChangeCase : PxExpressionImplement
{
    public StdTextChangeCase() : base("core.text.change_case") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
    {
        var text = call.Input("TEXT").ToText();

        var result = call.FieldText("CASE") switch
        {
            "UPPERCASE" => text.ToUpperInvariant(),
            "LOWERCASE" => text.ToLowerInvariant(),
            "TITLECASE" => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text.ToLowerInvariant()),
            _ => text
        };

        return ValueTask.FromResult<PxValue>(new PxStringValue(result));
    }
}

internal sealed class StdTextTrim : PxExpressionImplement
{
    public StdTextTrim() : base("core.text.trim") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
    {
        var text = call.Input("TEXT").ToText();

        var result = call.FieldText("MODE") switch
        {
            "LEFT" => text.TrimStart(),
            "RIGHT" => text.TrimEnd(),
            _ => text.Trim()
        };

        return ValueTask.FromResult<PxValue>(new PxStringValue(result));
    }
}

internal sealed class StdTextPrint : PxStatementImplement
{
    public StdTextPrint() : base("core.text.print") { }

    public override Task ExecuteAsync(PxContext context, PxCall call)
    {
        context.Print(call.Input("TEXT").ToText());
        return Task.CompletedTask;
    }
}
