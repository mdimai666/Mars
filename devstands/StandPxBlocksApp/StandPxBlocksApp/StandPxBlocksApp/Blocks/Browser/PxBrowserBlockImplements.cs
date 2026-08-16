using System.Text.Json;
using Mars.PxBlocks.Runtime.Execution;
using Mars.PxBlocks.Runtime.Values;
using static StandPxBlocksApp.Blocks.Browser.PxBrowserImplementHelpers;

namespace StandPxBlocksApp.Blocks.Browser;

/// <summary>
/// Исполнение браузерных блоков (PxBrowserBlocks) — только на сервере.
/// Каждая имплементация создаётся в момент запуска с инъекцией
/// <see cref="PxBrowserRunState"/> и логирует действие в панель вывода.
/// Ошибки Playwright маппятся в PxRuntimeException с id блока — редактор
/// подсвечивает блок, на котором сценарий упал.
/// </summary>
public sealed class PwGotoImplement(PxBrowserRunState state) : IPxStatementImplement
{
    public string TypeId => "demostand.playwright.goto";

    public async Task ExecuteAsync(PxContext context, PxCall call)
    {
        var url = TextInput(call, "URL");
        var page = await state.GetPageAsync();
        context.Print($"→ открываю страницу {url}");
        try
        {
            await page.GotoAsync(url);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw Fail(call, $"Не удалось открыть страницу {url}", exception);
        }
    }
}

public sealed class PwClickImplement(PxBrowserRunState state) : IPxStatementImplement
{
    public string TypeId => "demostand.playwright.click";

    public async Task ExecuteAsync(PxContext context, PxCall call)
    {
        var selector = TextInput(call, "SELECTOR");
        var page = await state.GetPageAsync();
        context.Print($"→ клик по селектору {selector}");
        try
        {
            await page.ClickAsync(selector);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw Fail(call, $"Клик по селектору {selector} не выполнен", exception);
        }
    }
}

public sealed class PwTypeImplement(PxBrowserRunState state) : IPxStatementImplement
{
    public string TypeId => "demostand.playwright.type";

    public async Task ExecuteAsync(PxContext context, PxCall call)
    {
        var text = TextInput(call, "TEXT");
        var selector = TextInput(call, "SELECTOR");
        var page = await state.GetPageAsync();
        context.Print($"→ ввожу «{text}» в {selector}");
        try
        {
            await page.FillAsync(selector, text);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw Fail(call, $"Ввод текста в {selector} не выполнен", exception);
        }
    }
}

public sealed class PwPressImplement(PxBrowserRunState state) : IPxStatementImplement
{
    public string TypeId => "demostand.playwright.press";

    public async Task ExecuteAsync(PxContext context, PxCall call)
    {
        var key = call.FieldText("KEY", "Enter");
        var page = await state.GetPageAsync();
        context.Print($"→ клавиша {key}");
        try
        {
            await page.Keyboard.PressAsync(key);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw Fail(call, $"Нажатие клавиши {key} не выполнено", exception);
        }
    }
}

public sealed class PwWaitSelectorImplement(PxBrowserRunState state) : IPxStatementImplement
{
    public string TypeId => "demostand.playwright.wait_selector";

    public async Task ExecuteAsync(PxContext context, PxCall call)
    {
        var selector = TextInput(call, "SELECTOR");
        var page = await state.GetPageAsync();
        context.Print($"→ жду элемент {selector}");
        try
        {
            await page.WaitForSelectorAsync(selector);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw Fail(call, $"Элемент {selector} не появился", exception);
        }
    }
}

/// <remarks>Браузер не нужен — пауза исполняется токеном запуска; state не используется.</remarks>
public sealed class PwWaitMsImplement : IPxStatementImplement
{
    public string TypeId => "demostand.playwright.wait_ms";

    public async Task ExecuteAsync(PxContext context, PxCall call)
    {
        var milliseconds = Math.Max(0, (int)call.FieldNumber("MS", 500));
        context.Print($"→ жду {milliseconds} мс");
        // С токеном запуска — Stop прерывает паузу сразу, не дожидаясь конца.
        await Task.Delay(milliseconds, context.CancellationToken);
    }
}

public sealed class PwGetTextImplement(PxBrowserRunState state) : IPxExpressionImplement
{
    public string TypeId => "demostand.playwright.get_text";

    public async ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
    {
        var selector = TextInput(call, "SELECTOR");
        var page = await state.GetPageAsync();
        try
        {
            var text = await page.InnerTextAsync(selector);
            context.Print($"→ текст {selector}: {Truncate(text)}");
            return new PxStringValue(text);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw Fail(call, $"Текст элемента {selector} не получен", exception);
        }
    }
}

public sealed class PwEvalJsImplement(PxBrowserRunState state) : IPxExpressionImplement
{
    public string TypeId => "demostand.playwright.eval_js";

    public async ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
    {
        var code = call.FieldText("CODE");
        if (string.IsNullOrWhiteSpace(code))
            throw new PxRuntimeException("Пустой код JavaScript", call.BlockId);

        var page = await state.GetPageAsync();
        try
        {
            var result = await page.EvaluateAsync(code);
            var value = PxValueJson.FromJson(JsonSerializer.SerializeToNode(result));
            context.Print($"→ JavaScript: {Truncate(value.ToText())}");
            return value;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw Fail(call, "Ошибка выполнения JavaScript", exception);
        }
    }
}

public sealed class PwPrintTextsImplement(PxBrowserRunState state) : IPxStatementImplement
{
    public string TypeId => "demostand.playwright.print_texts";

    public async Task ExecuteAsync(PxContext context, PxCall call)
    {
        var count = Math.Max(1, (int)call.FieldNumber("COUNT", 3));
        var selector = TextInput(call, "SELECTOR");
        var page = await state.GetPageAsync();
        context.Print($"→ первые {count} текстов по селектору {selector}:");
        try
        {
            var elements = await page.QuerySelectorAllAsync(selector);
            if (elements.Count == 0)
            {
                context.Print("ничего не найдено");
                return;
            }

            var index = 0;
            foreach (var element in elements.Take(count))
            {
                index++;
                context.Print($"[{index}] {Truncate(await element.InnerTextAsync())}");
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw Fail(call, $"Тексты по селектору {selector} не получены", exception);
        }
    }
}

/// <summary>Общие помощники браузерных имплементаций.</summary>
internal static class PxBrowserImplementHelpers
{
    /// <summary>Строка из value-входа; пустой сокет — ошибка у блока (подключите строку).</summary>
    public static string TextInput(PxCall call, string inputName)
    {
        if (!call.Inputs.TryGetValue(inputName, out var value))
            throw new PxRuntimeException($"Подключите строку к входу «{inputName}»", call.BlockId);
        return value.ToText();
    }

    /// <summary>Ошибка Playwright → ошибка исполнения с id блока.</summary>
    public static PxRuntimeException Fail(PxCall call, string message, Exception exception)
        => new($"{message}: {exception.Message}", call.BlockId);

    /// <summary>Короткое превью значения для лога.</summary>
    public static string Truncate(string text)
        => text.Length <= 200 ? text : text[..200] + "…";
}
