using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Mars.Core.Features;

public static class TextTool
{
    public static string Translit(string str)
    {
        string[] lat_up = { "A", "B", "V", "G", "D", "E", "Yo", "Zh", "Z", "I", "Y", "K", "L", "M", "N", "O", "P", "R", "S", "T", "U", "F", "Kh", "Ts", "Ch", "Sh", "Shch", "\"", "Y", "'", "E", "Yu", "Ya" };
        string[] lat_low = { "a", "b", "v", "g", "d", "e", "yo", "zh", "z", "i", "y", "k", "l", "m", "n", "o", "p", "r", "s", "t", "u", "f", "kh", "ts", "ch", "sh", "shch", "\"", "y", "'", "e", "yu", "ya" };
        string[] rus_up = { "А", "Б", "В", "Г", "Д", "Е", "Ё", "Ж", "З", "И", "Й", "К", "Л", "М", "Н", "О", "П", "Р", "С", "Т", "У", "Ф", "Х", "Ц", "Ч", "Ш", "Щ", "Ъ", "Ы", "Ь", "Э", "Ю", "Я" };
        string[] rus_low = { "а", "б", "в", "г", "д", "е", "ё", "ж", "з", "и", "й", "к", "л", "м", "н", "о", "п", "р", "с", "т", "у", "ф", "х", "ц", "ч", "ш", "щ", "ъ", "ы", "ь", "э", "ю", "я" };
        for (int i = 0; i <= 32; i++)
        {
            str = str.Replace(rus_up[i], lat_up[i]);
            str = str.Replace(rus_low[i], lat_low[i]);
        }
        return str;
    }

    static readonly Regex reg_whitespace = new Regex(@"\s+");
    static readonly Regex reg_nonValidSymbols = new Regex(@"[^\d\w-_.]");

    public static string TranslateToPostSlug(string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return "";
        string translited = Translit(str.Trim().ToLower());
        return reg_nonValidSymbols.Replace(reg_whitespace.Replace(translited, "_"), "");
    }

    static readonly Regex slugReg = new Regex(@"^[a-z\d_](?:[a-z\d-_.]*[a-z\d_])?$");
    static readonly Regex slugWithUpperReg = new Regex(@"^[A-Za-z\d_](?:[A-Za-z\d-_.]*[A-Za-z\d_])?$");

    public static bool IsValidSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return false;
        return slugReg.IsMatch(slug);
    }

    public static bool IsValidSlugWithUpperCase(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return false;
        return slugWithUpperReg.IsMatch(slug);
    }

    /// <summary>
    /// Отадет строку в виде = 'value1,True,null,,1'
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="ignoreInherits"></param>
    /// <returns></returns>
    public static string GetPropertiesValueAsString(object obj, bool ignoreInherits = true)
    {
        if (obj == null) return string.Empty;

        var flags = ignoreInherits ? (BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly) : (BindingFlags.Instance | BindingFlags.Public);

        var properties = obj.GetType()
                           .GetProperties(flags)
                           .Where(p => p.CanRead)
                           .Where(p => !p.GetCustomAttributes(typeof(JsonIgnoreAttribute), true).Any());

        var values = properties.Select(p => p.GetValue(obj)?.ToString() ?? "null");

        return string.Join(",", values);
    }
}
