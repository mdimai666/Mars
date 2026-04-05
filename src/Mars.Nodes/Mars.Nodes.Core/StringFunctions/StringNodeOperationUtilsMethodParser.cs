using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Mars.Nodes.Core.StringFunctions;

public static class StringNodeOperationUtilsMethodParser
{
    public static List<StringMethod> ParseMethods(Type type)
    {
        var methods = new List<StringMethod>();

        var methodInfos = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => (m.ReturnType == typeof(string) || m.ReturnType == typeof(string[])) &&
                       m.GetParameters().Length > 0);

        foreach (var methodInfo in methodInfos)
        {
            var firstParam = methodInfo.GetParameters().FirstOrDefault();
            if (firstParam == null) continue;

            if (firstParam.ParameterType == typeof(string) ||
                firstParam.ParameterType == typeof(string[]))
            {
                var parameters = methodInfo.GetParameters().Select(param => new MethodParameter
                {
                    Name = param.Name!,
                    Type = param.ParameterType,
                    DefaultValue = param.DefaultValue,
                    IsRequired = !param.HasDefaultValue,
                    Description = GetParameterDescription(param),
                    IsArray = param.ParameterType.IsArray,
                    ArrayElementType = param.ParameterType.IsArray ? param.ParameterType.GetElementType() : null
                }).ToArray();

                var stringMethod = new StringMethod
                {
                    Name = methodInfo.Name,
                    DisplayName = GetDisplayName(methodInfo),
                    GroupName = methodInfo.GetCustomAttribute<DisplayAttribute>()?.GroupName ?? "other",
                    MethodInfo = methodInfo,
                    Description = GetDescription(methodInfo),
                    ReturnType = methodInfo.ReturnType,
                    Parameters = parameters
                };

                methods.Add(stringMethod);
            }
        }

        return methods;
    }

    private static string GetDisplayName(MethodInfo method)
    {
        var attr = method.GetCustomAttribute<DisplayAttribute>();
        if (attr?.Name != null)
            return attr.Name!;

        return Regex.Replace(method.Name, "([A-Z])", " $1").Trim();
    }

    private static string? GetDescription(MethodInfo method)
    {
        var attr = method.GetCustomAttribute<DisplayAttribute>();
        return attr?.Description;
    }

    private static string GetParameterDescription(ParameterInfo param)
    {
        return param.Name!;
    }
}
