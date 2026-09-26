using System.Text.RegularExpressions;
using DynamicExpresso;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Core.Nodes.Common;

namespace Mars.Nodes.Expressions;

/// <summary>
/// Переиспользуемый резолвер полей ноды: Interpreter создаётся один раз, на каждое сообщение
/// переменные перепривязываются (RNS и msg пересоздаются на сообщение — NodeTaskJob.callbackNext).
/// Лестница движков: hot path (одиночный корневой путь) → NCalc (простые выражения) →
/// DynamicExpresso (кэш Lambda на bound-текст + типы параметров; полный Eval — fallback).
/// Арендуется через <see cref="ExpressionRunnerPool"/>, напрямую не хранится (в impl — только сессия).
/// </summary>
public sealed class ExpressionRunner
{
    Interpreter? _interpreter;
    readonly Dictionary<string, NCalc.Expression> _ncalcCache = [];
    readonly Dictionary<string, Lambda> _lambdaCache = [];
    readonly HashSet<string> _lambdaNoCache = [];

    // Остаточные ссылки на переменные интерпретатора в bound-тексте: кэшированная Lambda
    // захватывает их КОНСТАНТАМИ на момент Parse (DynamicExpresso печёт делегат с UsedParameters),
    // а обёртки контекстов перепривязываются на сообщение и могут смениться при редеплое flow —
    // такие выражения идут полным путём (BindRootPaths + Eval).
    static readonly Regex ResidualRootRegex = new(@"\b(msg|GlobalContext|FlowContext|VarNode)\b", RegexOptions.Compiled);

    public Interpreter GetInterpreter(ExpressionScope scope)
    {
        if (_interpreter is null)
            _interpreter = InputValueResolver.CreateInterpreter(scope.Rns, scope.Msg);
        else
            InputValueResolver.RebindVariables(_interpreter, scope);

        return _interpreter;
    }

    public object? Resolve(string kind, string value, string varType, ExpressionScope scope, Node node, string source)
    {
        if (kind == InputValueKind.Const)
            return InputValueResolver.ResolveConst(value, varType, node, source);

        var expression = kind switch
        {
            InputValueKind.Msg => $"msg.{value}",
            InputValueKind.Expression => value,
            _ => null
        };

        if (expression is not null)
        {
            if (TryResolveHotPath(expression, scope, out var hot))
                return InputValueResolver.ConvertToVarType(hot, expression, varType, node, source);

            if (TryResolveNCalc(expression, scope, out var ncalc))
                return InputValueResolver.ConvertToVarType(ncalc, expression, varType, node, source);

            if (TryResolveDeCached(expression, varType, scope, node, source, out var cached))
                return cached;
        }

        return InputValueResolver.Resolve(kind, value, varType, GetInterpreter(scope), scope, node, source);
    }

    // Одиночный корневой путь — без движка: тот же резолвинг, что BindRootPaths, но без regex-подмен,
    // интерпретатора и Expression.Compile. Неразрешившийся путь (null) уходит в DE — сохраняет его ошибки.
    static bool TryResolveHotPath(string path, ExpressionScope scope, out object? raw)
    {
        raw = null;

        if (!InputValueResolver.IsSingleRootPath(path))
            return false;

        // msg.Payload верхнего уровня — напрямую из NodeMsg, без DynamicNodeMsgWrapper
        // (его конструктор — рефлексия свойств payload). string/primitive/null-payload не имеет
        // собственного свойства "Payload", поэтому затенения обёртки здесь быть не может.
        if (path == MsgPayloadPath)
        {
            var payload = scope.Msg.Payload;
            if (payload is null or string || payload.GetType().IsPrimitive)
            {
                raw = payload;
                return true;
            }
        }

        return InputValueResolver.TryResolveRootPath(path, scope, out raw);
    }

    // Простое выражение (классификатор) — NCalc с кэшем Expression на текст. Значения корневых
    // путей резолвятся как в BindRootPaths; неразрешимый путь или ошибка NCalc → отказ и DE-путь
    // (сохраняет семантику и ошибки текущего поведения).
    bool TryResolveNCalc(string expression, ExpressionScope scope, out object? raw)
    {
        raw = null;

        if (!SimpleExpressionClassifier.TryGetForm(expression, out var form))
            return false;

        var values = new object[form.Paths.Length];
        for (var i = 0; i < form.Paths.Length; i++)
        {
            if (!InputValueResolver.TryResolveRootPath(form.Paths[i], scope, out var resolved) || resolved is null)
                return false;

            values[i] = resolved;
        }

        if (!_ncalcCache.TryGetValue(form.Text, out var ncalcExpression))
        {
            try
            {
                // NoStringTypeCoercion: без него NCalc приводит "7" + 1 к 8.0 (числовой коэрцией строки),
                // тогда как C# даёт "71"; с флагом смешанные string+number выражения падают в NCalc
                // и уходят в DE-путь (каноническая C#-семантика), string+string конкатенация сохраняется.
                ncalcExpression = new NCalc.Expression(form.Text,
                    NCalc.ExpressionOptions.StrictTypeMatching
                    | NCalc.ExpressionOptions.OrdinalStringComparer
                    | NCalc.ExpressionOptions.NoStringTypeCoercion);
                ncalcExpression.EvaluateBinary += CSharpIntegerDivision;
            }
            catch (Exception)
            {
                SimpleExpressionClassifier.Reject(expression);
                return false;
            }

            _ncalcCache[form.Text] = ncalcExpression;
        }

        for (var i = 0; i < form.Params.Length; i++)
            ncalcExpression.Parameters[form.Params[i]] = values[i];

        try
        {
            raw = ncalcExpression.Evaluate();
            return true;
        }
        catch (Exception)
        {
            SimpleExpressionClassifier.Reject(expression);
            raw = null;
            return false;
        }
    }

    // NCalc по умолчанию приводит `/` к floating-point (2/4 = 0.5) — тихое расхождение с C#.
    // Обработчик возвращает целочисленное деление по правилам двоичной промоции C#
    // (long доминирует, uint/uint → uint, uint с остальными → long, прочее целое → int);
    // double/float/decimal оставляет дефолтному пути NCalc (там семантика совпадает с C#).
    // ulong не обрабатывается (в значениях полей не встречается; VarNode-типы его не содержат).
    internal static void CSharpIntegerDivision(NCalc.Handlers.BinaryEventArgs args)
    {
        if (args.BinaryExpression.Type != NCalc.BinaryExpressionType.Div)
            return;

        var left = args.LeftValue();
        var right = args.RightValue();

        if (!IsIntegral(left) || !IsIntegral(right))
            return;

        if (left is long || right is long)
            args.Result = Convert.ToInt64(left) / Convert.ToInt64(right);
        else if (left is uint && right is uint)
            args.Result = Convert.ToUInt32(left) / Convert.ToUInt32(right);
        else if (left is uint || right is uint)
            args.Result = Convert.ToInt64(left) / Convert.ToInt64(right);
        else
            args.Result = Convert.ToInt32(left) / Convert.ToInt32(right);
    }

    static bool IsIntegral(object? value)
        => value is sbyte or byte or short or ushort or int or uint or long;

    // DE-путь с кэшем Lambda: корневые пути резолвятся в значения (CollectRootPaths), bound-текст
    // парсится один раз на (текст + типы значений), на сообщение только Invoke со свежими значениями.
    // Не кэшируется: тексты с остаточными ссылками на msg/контексты (см. ResidualRootRegex) и
    // тексты, упавшие при Parse (уходят полным путём — он даёт каноническую ошибку).
    bool TryResolveDeCached(string expression, string varType, ExpressionScope scope, Node node, string source, out object? result)
    {
        result = null;

        if (_lambdaNoCache.Contains(expression))
            return false;

        var collected = new List<InputValueResolver.RootPathBinding>(4);
        var boundText = InputValueResolver.CollectRootPaths(expression, scope, collected);

        if (ResidualRootRegex.IsMatch(boundText))
            return false;

        var cacheKey = BuildLambdaCacheKey(boundText, collected);

        if (!_lambdaCache.TryGetValue(cacheKey, out var lambda))
        {
            var interpreter = GetInterpreter(scope);
            var declared = new Parameter[collected.Count];
            for (var i = 0; i < collected.Count; i++)
                declared[i] = new Parameter(collected[i].Param, collected[i].Value.GetType());

            try
            {
                lambda = interpreter.Parse(boundText, typeof(object), declared);
            }
            catch (Exception)
            {
                _lambdaNoCache.Add(expression);
                return false; // полный путь бросит каноническую NodeExecuteException
            }

            _lambdaCache[cacheKey] = lambda;
        }

        object? raw;
        try
        {
            var args = new Parameter[collected.Count];
            for (var i = 0; i < collected.Count; i++)
                args[i] = new Parameter(collected[i].Param, collected[i].Value);

            raw = lambda.Invoke(args);
        }
        catch (Exception ex) when (ex is not NodeExecuteException)
        {
            throw new NodeExecuteException(node, $"{source}: expression '{expression}' cannot be evaluated: {ex.Message}", ex);
        }

        result = InputValueResolver.ConvertToVarType(raw, expression, varType, node, source);
        return true;
    }

    static string BuildLambdaCacheKey(string boundText, List<InputValueResolver.RootPathBinding> collected)
    {
        if (collected.Count == 0)
            return boundText;

        var key = new System.Text.StringBuilder(boundText);
        foreach (var binding in collected)
            key.Append('\u0000').Append(binding.Value.GetType().FullName);

        return key.ToString();
    }

    static readonly string MsgPayloadPath = $"msg.{nameof(NodeMsg.Payload)}";
}
