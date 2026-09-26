using System.Globalization;
using System.Text.RegularExpressions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Expressions;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Семантический тест-набор: одно и то же выражение через DynamicExpresso (продакшен-путь
/// InputValueResolver.ResolveExpression) и через NCalc (подстановка msg.*-путей параметрами).
/// Задача — найти выражения, валидные в обоих движках, но дающие РАЗНЫЙ результат (тихие
/// расхождения — опасный класс для классификатора), и зафиксировать границы применимости NCalc.
/// Запуск: Benchmark.NodeExpression.exe --semantic
/// </summary>
public static partial class SemanticComparison
{
    [GeneratedRegex(@"\bmsg\.([A-Za-z_][A-Za-z0-9_]*)")]
    private static partial Regex MsgPathRegex();

    static readonly (string Expr, string Group)[] Cases =
    [
        // A — кандидаты для NCalc (арифметика/сравнение/логика)
        ("msg.Payload", "A"),
        ("msg.count", "A"),
        ("msg.count * 2 + 1", "A"),
        ("msg.count > 40", "A"),
        ("msg.count > 40 && msg.flag", "A"),
        ("msg.count > 50 || msg.flag", "A"),
        ("!msg.flag", "A"),
        ("msg.Payload == \"hello\"", "A"),
        ("msg.Payload != \"hello\"", "A"),
        ("2 / 4", "A"),
        ("7 / 2", "A"),
        ("msg.count / 4", "A"),
        ("msg.count / 4 * 4", "A"),
        ("10 % 3", "A"),
        ("1.5 + 2", "A"),
        ("\"a\" == \"A\"", "A"),
        ("msg.Payload + \"!\"", "A"),
        ("msg.ts + 1000", "A"),
        ("msg.count + 0.5", "A"),
        ("true", "A"),
        ("1 == true", "A"),
        ("msg.missing == null", "A"),
        // B — C#-only (классификатор должен отсекать)
        ("msg.Payload.Count() + 1", "B"),
        ("msg.Payload.Length > 3", "B"),
        ("msg.Payload.Substring(0, 2)", "B"),
        ("msg.count > 10 ? \"big\" : \"small\"", "B"),
        ("string.IsNullOrEmpty(msg.Payload)", "B"),
        // C — NCalc-only синтаксис (в C# невалиден)
        ("msg.count > 40 and msg.flag", "C"),
    ];

    public static void Run()
    {
        var msg = new NodeMsg { Payload = "hello" };
        msg.Set("count", 42);
        msg.Set("flag", true);
        msg.Set("ts", 1758800000000L);
        msg.Set("topic", "sensor/1");

        var rns = new RuntimeNodeScopeMock(new ServiceCollection().BuildServiceProvider());
        var scope = new ExpressionScope(rns, msg);
        var node = new InjectNode();
        var wrapper = new DynamicNodeMsgWrapper(msg);

        Console.WriteLine("| # | Группа | Выражение | DynamicExpresso | NCalc | Вердикт |");
        Console.WriteLine("|--:|:-|---|---|---|---|");

        var i = 0;
        foreach (var (expr, group) in Cases)
        {
            i++;
            var de = EvalDynamicExpresso(expr, rns, msg, scope, node);
            var nc = EvalNCalc(expr, wrapper);
            Console.WriteLine($"| {i} | {group} | `{expr}` | {de.Text} | {nc.Text} | {Verdict(de, nc)} |");
        }
    }

    record EvalResult(bool Ok, object? Value, string Text);

    static EvalResult EvalDynamicExpresso(string expr, RuntimeNodeScopeMock rns, NodeMsg msg, ExpressionScope scope, Node node)
    {
        try
        {
            var interpreter = InputValueResolver.CreateInterpreter(rns, msg);
            var raw = InputValueResolver.ResolveExpression(expr, "", interpreter, scope, node, "semantic");
            return new EvalResult(true, raw, Format(raw));
        }
        catch (Exception ex)
        {
            return new EvalResult(false, null, "ERR: " + Short(ex));
        }
    }

    static EvalResult EvalNCalc(string expr, DynamicNodeMsgWrapper wrapper)
    {
        try
        {
            var converted = expr;
            var parameters = new Dictionary<string, object?>();

            foreach (Match m in MsgPathRegex().Matches(expr))
            {
                var name = "msg_" + m.Groups[1].Value;
                converted = converted.Replace(m.Value, name);
                parameters[name] = wrapper.GetValueByPath(m.Groups[1].Value);
            }

            var e = new NCalc.Expression(converted);
            foreach (var kv in parameters)
                e.Parameters[kv.Key] = kv.Value!;

            var raw = e.Evaluate();
            return new EvalResult(true, raw, Format(raw));
        }
        catch (Exception ex)
        {
            return new EvalResult(false, null, "ERR: " + Short(ex));
        }
    }

    static string Verdict(EvalResult de, EvalResult nc)
    {
        if (!de.Ok && !nc.Ok) return "BOTH-FAIL";
        if (!nc.Ok) return "NCalc-FAIL — безопасно, классификатор отсекает";
        if (!de.Ok) return "DE-FAIL — синтаксис NCalc-only";

        var deNum = ToDecimal(de.Value);
        var ncNum = ToDecimal(nc.Value);

        if (deNum is not null && ncNum is not null)
        {
            if (deNum != ncNum) return "❌ VALUE-DIFF";
            if (de.Value!.GetType() != nc.Value!.GetType()) return $"⚠️ TYPE-DIFF ({de.Value!.GetType().Name} vs {nc.Value!.GetType().Name})";
            return "MATCH";
        }

        if (Equals(de.Value, nc.Value) && de.Value?.GetType() == nc.Value?.GetType()) return "MATCH";
        if (Equals(de.Value?.ToString(), nc.Value?.ToString())) return $"⚠️ TYPE-DIFF ({de.Value?.GetType().Name ?? "null"} vs {nc.Value?.GetType().Name ?? "null"})";
        return "❌ VALUE-DIFF";
    }

    static decimal? ToDecimal(object? v) =>
        v is bool ? null : v as IConvertible is not null && v is not string && v is not char
            ? Convert.ToDecimal(v, CultureInfo.InvariantCulture)
            : null;

    static string Format(object? v)
    {
        if (v is null) return "`null`";
        var text = v switch
        {
            string s => $"\"{s}\"",
            bool b => b ? "true" : "false",
            IConvertible c => Convert.ToString(c, CultureInfo.InvariantCulture) ?? "?",
            _ => v.ToString() ?? "?"
        };
        return $"`{text}` ({v.GetType().Name})";
    }

    static string Short(Exception ex)
    {
        var msg = (ex.InnerException?.Message ?? ex.Message).Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        return msg.Length > 90 ? msg[..90] + "…" : msg;
    }
}
