using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using Mars.Core.Extensions;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes.InlineFunctions;

public static class InlineFunctionsUtilsMethodParser
{
    public static List<InlineFunctionNodeDefinition> ParseMethods(Type type, bool throwOnError = true)
    {
        var methods = new List<InlineFunctionNodeDefinition>();

        var methodInfos = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

        foreach (var methodInfo in methodInfos)
        {
            var displayAttr = methodInfo.GetCustomAttribute<DisplayAttribute>();
            var nodeAttr = methodInfo.GetCustomAttribute<MethodInlineFunctionNodeDefineAttribute>();

            var nodeTypeId = nodeAttr?.NodeTypeId?.AsNullIfEmpty() ?? $"parsed.{nameof(InlineFunctionNode)}.{type.Name}.{methodInfo.Name}";

            // Получаем типы всех параметров метода
            var paramTypes = methodInfo.GetParameters()
                                       .Select(p => p.ParameterType)
                                       .ToList();

            var isPrimitiveParams = paramTypes.All(p => p.IsPrimitive);

            if (!isPrimitiveParams)
            {
                if (throwOnError) throw new Exception($"{methodInfo.Name} has non-primitive parameters");
                continue;
            }

            // Формируем правильный тип делегата (Action или Func)
            Type delegateType;
            if (methodInfo.ReturnType == typeof(void))
            {
                // Если метод ничего не возвращает: Action или Action<T1, T2... >
                delegateType = Expression.GetDelegateType(paramTypes.ToArray());
            }
            else
            {
                // Если возвращает: последний аргумент в GetDelegateType всегда ReturnType
                paramTypes.Add(methodInfo.ReturnType);
                delegateType = Expression.GetDelegateType(paramTypes.ToArray());
            }

            // Создаем строго типизированный делегат для статического метода
            Delegate generatedDelegate = Delegate.CreateDelegate(delegateType, methodInfo);

            var inlineFunctionDefMethod = new InlineFunctionNodeDefinition()
            {
                TypeId = nodeTypeId,
                Name = methodInfo.Name,
                GroupName = displayAttr?.GroupName ?? "other",
                Delegate = generatedDelegate,
                Color = nodeAttr?.Color,
                Icon = nodeAttr?.Icon,
                Inputs = [new()],
                Outputs = [new()]
            };

            methods.Add(inlineFunctionDefMethod);
        }

        return methods;
    }

}
