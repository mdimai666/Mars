using System.Reflection;
using DynamicExpresso;
using Mars.Core.Extensions;
using Mars.Nodes.Abstractions;
using Mars.Nodes.Abstractions.Models;
using Mars.Nodes.Expressions;

namespace Mars.Nodes.Core.Implements.Nodes.Functions;

public class VariableSetNodeImpl : INodeImplement<VariableSetNode>
{

    public VariableSetNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public VariableSetNodeImpl(VariableSetNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        if (!Node.Setters.Any()) return Task.CompletedTask;

        using var expr = RNS.Expressions(Node);
        var ppt = expr.GetInterpreter(input);

        foreach (var setter in Node.Setters)
        {
            _ = SetExpression(setter, ppt, RNS, input);
        }

        callback(input);

        return Task.CompletedTask;
    }

    public static string SmartReplaceArrayInitializer(Type? type, string expression)
    {
        var isPureArrayInit = type?.IsArray ?? false && expression.StartsWith('[') && expression.EndsWith(']');
        var prepend = isPureArrayInit ? VarNode.GetPureArrayInitializerPrefix(type) : null;
        var value = prepend is null ? expression : ($"new {prepend} {{" + expression.TrimStart('[').TrimEnd(']') + "}");
        return value;
    }

    public static object? SetExpression(VariableSetExpression setter, Interpreter ppt, IRuntimeNodeScope RNS, NodeMsg input)
    {
        var segments = setter.ValuePath.Split(".");
        var valuePathRoot = segments[0];

        if (segments.Length < 2) throw new ArgumentException("Setter ValuePath segments count must greater than 1");

        //added because expression does not support ctor like : (int[])[1,2,3]
        var calcValue = (Type? type/*expectType*/) =>
        {
            var replaced = SmartReplaceArrayInitializer(type, setter.Expression);
            if (type == typeof(Guid) && replaced.Length == 38/*Guid.Length and quotes*/)
            {
                return new Guid(replaced.Trim('\"'));
            }
            return type is null
                ? ppt.Eval(replaced)
                : ppt.Eval(replaced, type);
        };

        var targetPropertyPath = setter.ValuePath.Substring(valuePathRoot.Length + 1);

        if (valuePathRoot == "msg")
        {
            var value = calcValue(null);
            var dmsg = new DynamicNodeMsgWrapper(input);
            dmsg.SetValueByPath(targetPropertyPath, value);
            return value;
        }
        else if (valuePathRoot == nameof(IRuntimeNodeScope.GlobalContext))
        {
            var value = calcValue(null); //TODO: вычислить какой исходный тип
            var prop = segments[1];
            if (segments.Length == 2)
            {
                if (RNS.GlobalContext.TryGetValue(prop, out var gVal))
                {
                    //gVal = value;
                    RNS.GlobalContext.SetValue(prop, value);
                }
                else
                {
                    RNS.GlobalContext.SetValue(prop, value);
                }
            }
            else
            {
                if (RNS.GlobalContext.TryGetValue(prop, out var gVal))
                {
                    SetProperty(gVal!, segments.Skip(2).JoinStr("."), value);
                }
                else
                {
                    throw new ArgumentException("cannot set by valuePath for not initialized");
                }
            }
            return value;
        }
        else if (valuePathRoot == nameof(IRuntimeNodeScope.FlowContext))
        {
            var value = calcValue(null); //TODO: вычислить какой исходный тип
            var prop = segments[1];
            if (segments.Length == 2)
            {
                if (RNS.FlowContext.TryGetValue(prop, out var fVal))
                {
                    //fVal = value;
                    RNS.FlowContext.SetValue(prop, value);
                }
                else
                {
                    RNS.FlowContext.SetValue(prop, value);
                }
            }
            else
            {
                if (RNS.FlowContext.TryGetValue(prop, out var fVal))
                {
                    SetProperty(fVal!, segments.Skip(2).JoinStr("."), value);
                }
                else
                {
                    throw new ArgumentException("cannot set by valuePath for not initialized");
                }
            }
            return value;
        }
        else if (valuePathRoot == nameof(VarNode))
        {
            var prop = segments[1];
            VarNodeVaribleDto? varDto = RNS.GetVarNodeVarible(prop);

            if (varDto is null) throw new ArgumentNullException($"VarNode '{prop}' not exist");

            //var value = calcValue(varDto.ArrayValue ? null : varDto.Value?.GetType());
            //var value = calcValue(varDto.Value?.GetType());
            var value = calcValue(VarNode.ResolveClrType(varDto.VarType));

            if (segments.Length == 2)
            {

                if (varDto != null)
                {
                    if (segments.Length == 2)
                    {
                        RNS.SetVarNodeVarible(prop, value);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    throw new ArgumentNullException($"VarNode '{prop}' not exist");
                }

            }
            else
            {
                if (RNS.FlowContext.TryGetValue(prop, out var fVal))
                {
                    SetProperty(fVal!, segments.Skip(2).JoinStr("."), value);
                }
                else
                {
                    throw new ArgumentException("cannot set by valuePath for not initialized");
                }
            }
            return value;

        }

        throw new NotImplementedException();
    }

#if false
    public static void SetProperty(object target, string propertyPath, object setTo)
    {
        var parts = propertyPath.Split('.');
        object currentObject = target;

        for (int i = 0; i < parts.Length; i++)
        {
            var prop = currentObject.GetType().GetProperty(parts[i]);
            if (i == parts.Length - 1)
            {
                // last property
                prop.SetValue(currentObject, setTo, null);
            }
            else
            {
                // not at the end, move to next object
                currentObject = prop.GetValue(currentObject)!;
            }
        }
    }
#else

    public static void SetProperty(object target, string propertyPath, object setTo)
    {
        var parts = propertyPath.Split('.');
        object currentObject = target;

        for (int i = 0; i < parts.Length; i++)
        {
            var member = currentObject.GetType().GetMember(parts[i], BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();

            if (member == null)
            {
                throw new ArgumentException($"Member '{parts[i]}' not found.");
            }

            if (i == parts.Length - 1)
            {
                // last member
                if (member is PropertyInfo prop)
                {
                    prop.SetValue(currentObject, setTo, null);
                }
                else if (member is FieldInfo field)
                {
                    field.SetValue(currentObject, setTo);
                }
            }
            else
            {
                // not at the end, move to next object
                if (member is PropertyInfo prop)
                {
                    currentObject = prop.GetValue(currentObject)!;
                }
                else if (member is FieldInfo field)
                {
                    currentObject = field.GetValue(currentObject)!;
                }
            }
        }
    }
#endif
}
