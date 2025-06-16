namespace Mars.MetaModelGenerator;

public static class GenSourceCodeMasterHelper
{
    public static string GetFormattedName(Type type)
    {
        if (type.IsGenericType)
        {
            string genericArguments = type.GetGenericArguments()
                                .Select(x => GetFriendlyTypeName(x.Name))
                                .Aggregate((x1, x2) => $"{x1}, {x2}");
            return $"{type.Name.Substring(0, type.Name.IndexOf("`"))}"
                 + $"<{genericArguments}>";
        }
        return type.Name;
    }

    public static string GetFriendlyTypeName(string typeName)
    {
        return typeName switch
        {
            "Object" => "object",
            "String" => "string",
            "Boolean" => "bool",
            "Byte" => "byte",
            "Char" => "char",
            "Decimal" => "decimal",
            "Double" => "double",
            "Int16" => "short",
            "Int32" => "int",
            "Int64" => "long",
            "SByte" => "sbyte",
            "Single" => "float",
            "UInt16" => "ushort",
            "UInt32" => "uint",
            "UInt64" => "ulong",
            "Void" => "void",
            _ => typeName
        };
    }

    public static string AddTabsToLines(string input, int tabsCount = 1)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        string tabs = new string('\t', tabsCount);
        string[] lines = input.Split('\n', StringSplitOptions.None);

        return string.Join("\n",
            lines.Select((line, index) => index == 0 ? line : tabs + line));
    }

    public static string GetNormalizedTypeName(string typeName, string suffix = "Mto")
    {
        return char.ToUpper(typeName[0]) + typeName.Substring(1) + suffix;
    }
}
