using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using Mars.Nodes.Core;

namespace Mars.Nodes.Expressions;

/// <summary>
/// Классификатор «простых» выражений для NCalc-пути: корневые пути, литералы, + - * / %,
/// сравнения, && || !, скобки. Консервативен: вызовы, `? :`, NCalc-only `and`/`or` — НЕ пропускает,
/// они уходят в DynamicExpresso. `/` пропускает: C#-семантика целочисленного деления
/// обеспечивается обработчиком <see cref="ExpressionRunner.CSharpIntegerDivision"/>
/// (без него NCalc всегда даёт floating-point — тихое расхождение с C#, см. SEMANTIC.md).
/// Решение и конвертация кэшируются на текст выражения.
/// Правила выведены из benchmarks/Benchmark.NodeExpression/SEMANTIC.md (2026-09-26).
/// </summary>
public static class SimpleExpressionClassifier
{
    public sealed record ConvertedForm(string Text, string[] Paths, string[] Params);

    static readonly ConvertedForm Rejected = new("", [], []);

    static readonly ConcurrentDictionary<string, ConvertedForm> Cache = new();

    static readonly Regex TokenRegex = new(
        @"\s+"
        + @"|""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'"
        + $@"|(?:{string.Join("|", ValuePath.Roots)})(?:\.[A-Za-z_][A-Za-z0-9_]*)+"
        + @"|true|false|null"
        + @"|\d+(?:\.\d+)?(?:[eE][+-]?\d+)?[fFdDmMlLuU]{0,2}"
        + @"|&&|\|\||==|!=|>=|<=|[><]"
        + @"|[+\-*/%()!]",
        RegexOptions.Compiled);

    public static bool IsSimple(string expression) => TryGetForm(expression, out _);

    public static bool TryGetForm(string expression, out ConvertedForm form)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            form = Rejected;
            return false;
        }

        var cached = Cache.GetOrAdd(expression, Convert);
        form = cached;
        return !ReferenceEquals(cached, Rejected);
    }

    /// <summary>Парсер/eval NCalc не смог выражение — больше не пускать его в NCalc.</summary>
    public static void Reject(string expression) => Cache[expression] = Rejected;

    static ConvertedForm Convert(string expression)
    {
        var sb = new StringBuilder(expression.Length);
        var paths = new List<string>();
        var paramNames = new List<string>();
        var pos = 0;
        string? prev = null;

        foreach (Match token in TokenRegex.Matches(expression))
        {
            if (token.Index != pos)
                return Rejected; // непокрытый символ — не простое выражение

            var text = token.Value;

            if (text == "(" && prev is not null && !IsGroupingAllowedAfter(prev))
                return Rejected; // вызов вида path(...) / number(...) — не группировка

            if (IsRootPath(text))
            {
                var param = text.Replace('.', '_');
                if (!paths.Contains(text))
                {
                    paths.Add(text);
                    paramNames.Add(param);
                }

                sb.Append(param);
            }
            else
            {
                sb.Append(text);
            }

            prev = text;
            pos = token.Index + token.Length;
        }

        if (pos != expression.Length)
            return Rejected;

        return new ConvertedForm(sb.ToString(), [.. paths], [.. paramNames]);
    }

    // `(` разрешена только после операторов и другой `(` — всё остальное означает вызов
    static bool IsGroupingAllowedAfter(string prev)
        => prev is "(" or "!" or "+" or "-" or "*" or "%" or "&&" or "||" or "==" or "!=" or ">" or ">=" or "<" or "<=";

    static bool IsRootPath(string token)
    {
        var dot = token.IndexOf('.');
        return dot > 0 && ValuePath.Roots.Contains(token[..dot]);
    }
}
