using System.Dynamic;
using System.Reflection;
using Mars.Core.Extensions;
using Mars.Host.Shared.Templators;
using Mars.Nodes.Core.Implements.Models;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class VariableSetNodeImpl : INodeImplement<VariableSetNode>, INodeImplement
{

    public VariableSetNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public VariableSetNodeImpl(VariableSetNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, Action<Exception> Error)
    {
        if (!Node.Setters.Any()) return Task.CompletedTask;

        var ppt = CreateInterpreter(RED, input);

        foreach (var setter in Node.Setters)
        {
            _ = SetExpression(setter, ppt, RED, input);
        }

        callback(input);

        return Task.CompletedTask;
    }

    class ContextPropertyAccesableObject : DynamicObject
    {
        private readonly VariablesContextDictionary _dict;

        public ContextPropertyAccesableObject(VariablesContextDictionary dict)
        {
            _dict = dict;
        }

        // установка свойства
        //public override bool TrySetMember(SetMemberBinder binder, object? value)
        //{
        //    if (value is not null)
        //    {
        //        members[binder.Name] = value;
        //        return true;
        //    }
        //    return false;
        //}

        // получение свойства
        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            //result = null;
            //if (members.ContainsKey(binder.Name))
            //{
            //    result = members[binder.Name];
            //    return true;
            //}
            //return false;
            return _dict.TryGetValue(binder.Name, out result);
        }

        // вызов метода
        //public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
        //{
        //    result = null;
        //    if (args?[0] is int number)
        //    {
        //        // получаем метод по имен
        //        dynamic method = members[binder.Name];
        //        // вызываем метод, передавая его параметру значение args?[0]
        //        result = method(number);
        //    }
        //    // если result не равен null, то вызов метода прошел успешно
        //    return result != null;
        //}
    }

    class ContextVarNodesAccesableObject : DynamicObject
    {
        private readonly IReadOnlyDictionary<string, VarNode> _dict;

        public ContextVarNodesAccesableObject(IReadOnlyDictionary<string, VarNode> _varNodesDict)
        {
            _dict = _varNodesDict;
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

    public static XInterpreter CreateInterpreter(IRED RED, NodeMsg input)
    {
        //var globalContext = new ExpandoObject();
        //foreach(var key in RED.GlobalContext.Keys)
        //{
        //    globalContext.
        //}
        var globalContext = new ContextPropertyAccesableObject(RED.GlobalContext);
        var flowContext = new ContextPropertyAccesableObject(RED.FlowContext);
        var varNodexContext = new ContextVarNodesAccesableObject(RED.VarNodesDict);

        var executionContext = new Dictionary<string, object>()
        {
            //[nameof(RED.GlobalContext)] = RED.GlobalContext,
            //[nameof(RED.FlowContext)] = RED.FlowContext,
            [nameof(RED.GlobalContext)] = globalContext,
            [nameof(RED.FlowContext)] = flowContext,
            [nameof(VarNode)] = varNodexContext,
            ["msg"] = input,
        };

        return new XInterpreter(null, executionContext);
    }

    public static string SmartReplaceArrayInitializer(Type? type, string expression)
    {
        var isPureArrayInit = type?.IsArray ?? false && expression.StartsWith('[') && expression.EndsWith(']');
        var prepend = isPureArrayInit ? VarNode.GetPureArrayInitializerPrefix(type) : null;
        var value = prepend is null ? expression : ($"new {prepend} {{" + expression.TrimStart('[').TrimEnd(']') + "}");
        return value;
    }

    public static object? SetExpression(VariableSetExpression setter, XInterpreter ppt, IRED RED, NodeMsg input)
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
                ? ppt.Get.Eval(replaced)
                : ppt.Get.Eval(replaced, type);
        };

        var targetPropertyPath = setter.ValuePath.Substring(valuePathRoot.Length + 1);

        if (valuePathRoot == "msg")
        {
            //var value = calcValue(input.Payload?.GetType());
            var value = calcValue(null);
            SetProperty(input, targetPropertyPath, value);
            return value;
        }
        else if (valuePathRoot == nameof(IRED.GlobalContext))
        {
            var value = calcValue(null); //TODO: вычислить какой исходный тип
            var prop = segments[1];
            if (segments.Length == 2)
            {
                if (RED.GlobalContext.TryGetValue(prop, out var gVal))
                {
                    //gVal = value;
                    RED.GlobalContext.SetValue(prop, value);
                }
                else
                {
                    RED.GlobalContext.SetValue(prop, value);
                }
            }
            else
            {
                if (RED.GlobalContext.TryGetValue(prop, out var gVal))
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
        else if (valuePathRoot == nameof(IRED.FlowContext))
        {
            var value = calcValue(null); //TODO: вычислить какой исходный тип
            var prop = segments[1];
            if (segments.Length == 2)
            {
                if (RED.FlowContext.TryGetValue(prop, out var fVal))
                {
                    //fVal = value;
                    RED.FlowContext.SetValue(prop, value);
                }
                else
                {
                    RED.FlowContext.SetValue(prop, value);
                }
            }
            else
            {
                if (RED.FlowContext.TryGetValue(prop, out var fVal))
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
            VarNodeVaribleDto? varDto = RED.GetVarNodeVarible(prop);

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
                        RED.SetVarNodeVarible(prop, value);
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
                if (RED.FlowContext.TryGetValue(prop, out var fVal))
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
