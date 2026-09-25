using System.Dynamic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DynamicExpresso;
using Mars.Nodes.Abstractions;
using Mars.Nodes.Abstractions.Models;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Core.Nodes.Common;

namespace Mars.Nodes.Expressions;

public class ContextPropertyAccesableObject : DynamicObject
{
    private readonly VariablesContextDictionary _dict;

    public ContextPropertyAccesableObject(VariablesContextDictionary dict)
    {
        _dict = dict;
    }

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        return _dict.TryGetValue(binder.Name, out result);
    }
}

public class ContextVarNodesAccesableObject : DynamicObject
{
    private readonly IReadOnlyDictionary<string, VarNode> _dict;

    public ContextVarNodesAccesableObject(IReadOnlyDictionary<string, VarNode> dict)
    {
        _dict = dict;
    }

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        if (_dict.TryGetValue(binder.Name, out var varNode))
        {
            result = varNode.Value;
            return true;
        }

        result = null;
        return false;
    }
}

public readonly record struct ExpressionScope(IRuntimeNodeScope Rns, NodeMsg Msg);

public static class InputValueResolver
{
    static readonly Regex RootPathRegex = new(
        $@"\b({string.Join("|", ValuePath.Roots)})(?:\.[A-Za-z_][A-Za-z0-9_]*(?![A-Za-z0-9_(]))+",
        RegexOptions.Compiled);

    static readonly Regex SingleRootPathRegex = new(
        $@"^(?:{string.Join("|", ValuePath.Roots)})(?:\.[A-Za-z_][A-Za-z0-9_]*)+$",
        RegexOptions.Compiled);

    public static bool IsSingleRootPath(string path) => SingleRootPathRegex.IsMatch(path);

    public static Interpreter CreateInterpreter(IRuntimeNodeScope rns, NodeMsg? input = null)
    {
        return CreateInterpreter(rns.GlobalContext, rns.FlowContext, rns.VarNodesDict, input);
    }

    public static Interpreter CreateInterpreter(VariablesContextDictionary globalContext,
                                                VariablesContextDictionary? flowContext,
                                                IReadOnlyDictionary<string, VarNode> varNodesDict,
                                                NodeMsg? input = null)
    {
        var interpreter = new Interpreter(InterpreterOptions.Default | InterpreterOptions.LateBindObject);
        interpreter.Reference(typeof(Enumerable));
        interpreter.SetVariable("env", (string key) => Environment.GetEnvironmentVariable(key));

        RebindVariables(interpreter, globalContext, flowContext, varNodesDict, input);

        return interpreter;
    }

    public static void RebindVariables(Interpreter interpreter, ExpressionScope scope)
        => RebindVariables(interpreter, scope.Rns.GlobalContext, scope.Rns.FlowContext, scope.Rns.VarNodesDict, scope.Msg);

    public static void RebindVariables(Interpreter interpreter,
                                       VariablesContextDictionary globalContext,
                                       VariablesContextDictionary? flowContext,
                                       IReadOnlyDictionary<string, VarNode> varNodesDict,
                                       NodeMsg? input)
    {
        interpreter.SetVariable(nameof(IRuntimeNodeScope.GlobalContext), new ContextPropertyAccesableObject(globalContext));
        interpreter.SetVariable(nameof(IRuntimeNodeScope.FlowContext), new ContextPropertyAccesableObject(flowContext ?? new()));
        interpreter.SetVariable(nameof(VarNode), new ContextVarNodesAccesableObject(varNodesDict));
        if (input is not null)
            interpreter.SetVariable("msg", new DynamicNodeMsgWrapper(input));
    }

    public static object? Resolve(string kind, string value, string varType, Interpreter? interpreter, ExpressionScope scope, Node node, string source)
    {
        return kind switch
        {
            InputValueKind.Const => ResolveConst(value, varType, node, source),
            InputValueKind.Msg => ResolveExpression($"msg.{value}", varType, interpreter, scope, node, source),
            InputValueKind.Expression => ResolveExpression(value, varType, interpreter, scope, node, source),
            _ => throw new NodeExecuteException(node, $"{source}: unknown value kind '{kind}'."),
        };
    }

    public static object? ResolveConst(string value, string varType, Node node, string source)
    {
        if (string.IsNullOrEmpty(varType))
            return value;

        if (varType == VarNode.TimestampTypeName)
            return ResolveTimestampConst(value, node, source);

        if (varType == "string")
            return value;

        try
        {
            return JsonSerializer.Deserialize(value, VarNode.ResolveClrType(varType));
        }
        catch (JsonException ex)
        {
            throw new NodeExecuteException(node, $"{source}: value '{value}' is not a valid {varType}.", ex);
        }
    }

    public static object? ResolveExpression(string value, string varType, Interpreter? interpreter, ExpressionScope scope, Node node, string source)
    {
        if (interpreter is null)
            throw new NodeExecuteException(node, $"{source}: expression interpreter is not initialized.");

        object? raw;
        var bound = BindRootPaths(value, interpreter, scope);
        try
        {
            raw = interpreter.Eval(bound);
        }
        catch (Exception ex) when (ex is not NodeExecuteException)
        {
            throw new NodeExecuteException(node, $"{source}: expression '{value}' cannot be evaluated: {ex.Message}", ex);
        }

        return ConvertToVarType(raw, value, varType, node, source);
    }

    // Корневые пути подменяются параметрами со статическим типом: на dynamic-приёмнике
    // C#-байндер не resolve'ит extension-методы LINQ (msg.Payload.Count()).
    public static string BindRootPaths(string expression, Interpreter interpreter, ExpressionScope scope)
    {
        var collected = new List<RootPathBinding>(4);
        var bound = CollectRootPaths(expression, scope, collected);

        foreach (var binding in collected)
            interpreter.SetVariable(binding.Param, binding.Value);

        return bound;
    }

    // То же, что BindRootPaths, но без interpreter: собранные значения отдаёт списком
    // (для кэша Lambda с объявленными параметрами — ExpressionRunner).
    internal static string CollectRootPaths(string expression, ExpressionScope scope, List<RootPathBinding> collected)
    {
        var matches = RootPathRegex.Matches(expression);
        if (matches.Count == 0)
            return expression;

        var literalMask = BuildStringLiteralMask(expression);
        var sb = new StringBuilder(expression.Length);
        var last = 0;

        foreach (Match match in matches)
        {
            if (literalMask[match.Index] || (match.Index > 0 && expression[match.Index - 1] == '.'))
                continue;

            if (!TryResolveRootPath(match.Value, scope, out var value) || value is null)
                continue;

            var paramName = match.Value.Replace('.', '_');
            if (collected.All(b => b.Param != paramName))
                collected.Add(new RootPathBinding(paramName, value));

            sb.Append(expression, last, match.Index - last);
            sb.Append(paramName);
            last = match.Index + match.Length;
        }

        sb.Append(expression, last, expression.Length - last);
        return sb.ToString();
    }

    internal readonly record struct RootPathBinding(string Param, object Value);

    internal static bool TryResolveRootPath(string path, ExpressionScope scope, out object? value)
    {
        value = null;
        var dot = path.IndexOf('.');
        var root = path[..dot];
        var rest = path[(dot + 1)..];

        switch (root)
        {
            case "msg":
                value = new DynamicNodeMsgWrapper(scope.Msg).GetValueByPath(rest);
                return value is not null;
            case nameof(IRuntimeNodeScope.GlobalContext):
                return scope.Rns.GlobalContext.TryGetValue(rest, out value) && value is not null;
            case nameof(IRuntimeNodeScope.FlowContext):
                return scope.Rns.FlowContext is not null
                       && scope.Rns.FlowContext.TryGetValue(rest, out value)
                       && value is not null;
            case nameof(VarNode):
                if (!scope.Rns.VarNodesDict.TryGetValue(rest, out var varNode) || varNode.Value is null)
                    return false;
                value = varNode.Value;
                return true;
            default:
                return false;
        }
    }

    static bool[] BuildStringLiteralMask(string s)
    {
        var mask = new bool[s.Length];
        var quote = default(char);

        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];

            if (quote == default(char))
            {
                if (c is '"' or '\'')
                {
                    quote = c;
                    mask[i] = true;
                }
            }
            else
            {
                mask[i] = true;

                if (c == '\\' && i + 1 < s.Length)
                {
                    mask[i + 1] = true;
                    i++;
                }
                else if (c == quote)
                {
                    quote = default(char);
                }
            }
        }

        return mask;
    }

    static long ResolveTimestampConst(string value, Node node, string source)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DateTimeOffset.Now.ToUnixTimeMilliseconds();

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var millis))
            return millis;

        throw new NodeExecuteException(node, $"{source}: value '{value}' is not a valid timestamp (empty or unix millis expected).");
    }

    internal static object? ConvertToVarType(object? raw, string expression, string varType, Node node, string source)
    {
        if (string.IsNullOrEmpty(varType))
            return raw;

        var target = VarNode.ResolveClrType(varType);

        if (target == typeof(string))
            return raw?.ToString() ?? "";

        if (raw is null)
            throw new NodeExecuteException(node, $"{source}: expression '{expression}' returned null, but {varType} is expected.");

        if (target.IsInstanceOfType(raw))
            return raw;

        if (raw is IConvertible && !target.IsArray)
        {
            try
            {
                return Convert.ChangeType(raw, target, CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
            {
                throw new NodeExecuteException(node, $"{source}: expression result '{raw}' cannot be converted to {varType}.", ex);
            }
        }

        try
        {
            return JsonSerializer.Deserialize(JsonSerializer.Serialize(raw), target);
        }
        catch (JsonException ex)
        {
            throw new NodeExecuteException(node, $"{source}: expression result '{raw}' cannot be converted to {varType}.", ex);
        }
    }
}
