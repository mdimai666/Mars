using Microsoft.CodeAnalysis.CSharp;

namespace Mars.MetaModelGenerator;

public static class GenSourceCodeMasterHelper
{
    /// <summary>
    /// Имя C#-свойства из ключа метаполя: ключевые слова экранируются '@'
    /// (runtime-имя свойства остаётся исходным ключом).
    /// </summary>
    public static string EscapeCSharpKeyword(string name)
        => SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;

    public static string GetFormattedName(Type type)
    {
        var nullableUnderlying = Nullable.GetUnderlyingType(type);
        if (nullableUnderlying is not null)
        {
            return GetFriendlyTypeName(nullableUnderlying.Name) + "?";
        }

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

        string tabs = new('\t', tabsCount);
        string[] lines = input.Split('\n', StringSplitOptions.None);

        return string.Join("\n",
            lines.Select((line, index) => index == 0 ? line : tabs + line));
    }

    /// <summary>
    /// Имя Mto-класса из имени пост-типа: сегменты через не-буквенно-цифровые символы
    /// поднимаются в PascalCase ("temp-page" → "TempPageMto") — дефис валиден в TypeName,
    /// но невалиден в имени C#-класса.
    /// </summary>
    public static string GetNormalizedTypeName(string typeName, string suffix = "Mto")
    {
        var separators = typeName.Where(c => !char.IsAsciiLetterOrDigit(c)).Distinct().ToArray();
        var parts = typeName.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(p => char.ToUpper(p[0]) + p.Substring(1))) + suffix;
    }
}
