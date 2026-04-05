using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Mars.Nodes.Core.Resources;

namespace Mars.Nodes.Core.StringFunctions;

public static class StringNodeOperationUtils
{
    // ========== Базовые преобразования ==========

    [Display(Name = "В верхний регистр", Description = "Преобразует все символы в верхний регистр", GroupName = "basic", ResourceType = typeof(NodeRes))]
    public static string ToUpper(string input) => input.ToUpper();

    [Display(Name = "В нижний регистр", Description = "Преобразует все символы в нижний регистр", GroupName = "basic", ResourceType = typeof(NodeRes))]
    public static string ToLower(string input) => input.ToLower();

    [Display(Name = "Капитализация", Description = "Первая буква заглавная, остальные строчные", GroupName = "basic", ResourceType = typeof(NodeRes))]
    public static string Capitalize(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpper(input[0]) + input.Substring(1).ToLower();
    }

    [Display(Name = "Капитализация каждого слова", Description = "Каждое слово начинается с заглавной буквы", GroupName = "basic", ResourceType = typeof(NodeRes))]
    public static string ToTitleCase(string input)
    {
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLower());
    }

    [Display(Name = "Инвертировать регистр", Description = "Меняет регистр каждого символа", GroupName = "basic", ResourceType = typeof(NodeRes))]
    public static string InvertCase(string input)
    {
        return new string(input.Select(c => char.IsUpper(c) ? char.ToLower(c) : char.ToUpper(c)).ToArray());
    }

    // ========== Добавление строк ==========

    [Display(Name = "Добавить в начало", Description = "Добавляет строку в начало", GroupName = "add", ResourceType = typeof(NodeRes))]
    public static string Prepend(string input, string text) => text + input;

    [Display(Name = "Добавить в конец", Description = "Добавляет строку в конец", GroupName = "add", ResourceType = typeof(NodeRes))]
    public static string Append(string input, string text) => input + text;

    [Display(Name = "Добавить если отсутствует в начале", Description = "Добавляет префикс, если его нет", GroupName = "add", ResourceType = typeof(NodeRes))]
    public static string PrependIfNotExists(string input, string prefix, bool ignoreCase = false)
    {
        if (string.IsNullOrEmpty(input)) return prefix;
        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return input.StartsWith(prefix, comparison) ? input : prefix + input;
    }

    [Display(Name = "Добавить если отсутствует в конце", Description = "Добавляет суффикс, если его нет", GroupName = "add", ResourceType = typeof(NodeRes))]
    public static string AppendIfNotExists(string input, string suffix, bool ignoreCase = false)
    {
        if (string.IsNullOrEmpty(input)) return suffix;
        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return input.EndsWith(suffix, comparison) ? input : input + suffix;
    }

    [Display(Name = "Вставить в позицию", Description = "Вставляет строку в указанную позицию", GroupName = "add", ResourceType = typeof(NodeRes))]
    public static string InsertAt(string input, int index, string text)
    {
        if (index < 0) index = 0;
        if (index > input.Length) index = input.Length;
        return input.Insert(index, text);
    }

    [Display(Name = "Заполнить слева", Description = "Дополняет строку слева до указанной длины", GroupName = "add", ResourceType = typeof(NodeRes))]
    public static string PadLeft(string input, int totalWidth, char paddingChar = ' ')
    {
        return input.PadLeft(totalWidth, paddingChar);
    }

    [Display(Name = "Заполнить справа", Description = "Дополняет строку справа до указанной длины", GroupName = "add", ResourceType = typeof(NodeRes))]
    public static string PadRight(string input, int totalWidth, char paddingChar = ' ')
    {
        return input.PadRight(totalWidth, paddingChar);
    }

    // ========== Удаление и обрезка ==========

    [Display(Name = "Обрезать пробелы", Description = "Удаляет пробелы в начале и конце строки", GroupName = "remove", ResourceType = typeof(NodeRes))]
    public static string Trim(string input) => input.Trim();

    [Display(Name = "Обрезать слева", Description = "Удаляет пробелы в начале строки", GroupName = "remove", ResourceType = typeof(NodeRes))]
    public static string TrimStart(string input) => input.TrimStart();

    [Display(Name = "Обрезать справа", Description = "Удаляет пробелы в конце строки", GroupName = "remove", ResourceType = typeof(NodeRes))]
    public static string TrimEnd(string input) => input.TrimEnd();

    [Display(Name = "Удалить префикс", Description = "Удаляет указанный префикс, если он есть", GroupName = "remove", ResourceType = typeof(NodeRes))]
    public static string RemovePrefix(string input, string prefix, bool ignoreCase = false)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return input.StartsWith(prefix, comparison) ? input.Substring(prefix.Length) : input;
    }

    [Display(Name = "Удалить суффикс", Description = "Удаляет указанный суффикс, если он есть", GroupName = "remove", ResourceType = typeof(NodeRes))]
    public static string RemoveSuffix(string input, string suffix, bool ignoreCase = false)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return input.EndsWith(suffix, comparison) ? input.Substring(0, input.Length - suffix.Length) : input;
    }

    [Display(Name = "Удалить диапазон", Description = "Удаляет символы с указанной позиции", GroupName = "remove", ResourceType = typeof(NodeRes))]
    public static string RemoveRange(string input, int startIndex, int length)
    {
        if (startIndex < 0) startIndex = 0;
        if (startIndex >= input.Length) return input;
        if (startIndex + length > input.Length) length = input.Length - startIndex;
        return input.Remove(startIndex, length);
    }

    [Display(Name = "Удалить все пробелы", Description = "Удаляет все пробельные символы", GroupName = "remove", ResourceType = typeof(NodeRes))]
    public static string RemoveWhitespace(string input)
    {
        return new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }

    [Display(Name = "Удалить повторяющиеся пробелы", Description = "Заменяет множественные пробелы на один", GroupName = "remove", ResourceType = typeof(NodeRes))]
    public static string RemoveDuplicateSpaces(string input)
    {
        return Regex.Replace(input, @"\s+", " ");
    }

    // ========== Извлечение и получение частей ==========

    [Display(Name = "Получить первые N символов", Description = "Возвращает указанное количество символов с начала", GroupName = "extract", ResourceType = typeof(NodeRes))]
    public static string Left(string input, int length)
    {
        if (length <= 0) return "";
        if (length >= input.Length) return input;
        return input.Substring(0, length);
    }

    [Display(Name = "Получить последние N символов", Description = "Возвращает указанное количество символов с конца", GroupName = "extract", ResourceType = typeof(NodeRes))]
    public static string Right(string input, int length)
    {
        if (length <= 0) return "";
        if (length >= input.Length) return input;
        return input.Substring(input.Length - length);
    }

    [Display(Name = "Получить подстроку", Description = "Возвращает подстроку начиная с указанной позиции", GroupName = "extract", ResourceType = typeof(NodeRes))]
    public static string Substring(string input, int startIndex, int? length = null)
    {
        if (startIndex < 0) startIndex = 0;
        if (startIndex >= input.Length) return "";
        return length.HasValue ? input.Substring(startIndex, Math.Min(length.Value, input.Length - startIndex))
                               : input.Substring(startIndex);
    }

    [Display(Name = "Между", Description = "Возвращает текст между двумя маркерами", GroupName = "extract", ResourceType = typeof(NodeRes))]
    public static string Between(string input, string start, string end)
    {
        int startIndex = input.IndexOf(start);
        if (startIndex == -1) return "";
        startIndex += start.Length;

        int endIndex = input.IndexOf(end, startIndex);
        if (endIndex == -1) return "";

        return input.Substring(startIndex, endIndex - startIndex);
    }

    [Display(Name = "До первого вхождения", Description = "Возвращает текст до указанной подстроки", GroupName = "extract", ResourceType = typeof(NodeRes))]
    public static string Before(string input, string value)
    {
        int index = input.IndexOf(value);
        return index == -1 ? input : input.Substring(0, index);
    }

    [Display(Name = "После первого вхождения", Description = "Возвращает текст после указанной подстроки", GroupName = "extract", ResourceType = typeof(NodeRes))]
    public static string After(string input, string value)
    {
        int index = input.IndexOf(value);
        return index == -1 ? "" : input.Substring(index + value.Length);
    }

    [Display(Name = "До последнего вхождения", Description = "Возвращает текст до последнего вхождения подстроки", GroupName = "extract", ResourceType = typeof(NodeRes))]
    public static string BeforeLast(string input, string value)
    {
        int index = input.LastIndexOf(value);
        return index == -1 ? input : input.Substring(0, index);
    }

    [Display(Name = "После последнего вхождения", Description = "Возвращает текст после последнего вхождения подстроки", GroupName = "extract", ResourceType = typeof(NodeRes))]
    public static string AfterLast(string input, string value)
    {
        int index = input.LastIndexOf(value);
        return index == -1 ? "" : input.Substring(index + value.Length);
    }

    // ========== Замена ==========

    [Display(Name = "Замена", Description = "Заменяет все вхождения подстроки", GroupName = "replace", ResourceType = typeof(NodeRes))]
    public static string Replace(string input, string oldValue, string newValue, bool ignoreCase = false)
        => input.Replace(oldValue, newValue, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    [Display(Name = "RegExp замена", Description = "Заменяет по регулярному выражению", GroupName = "replace", ResourceType = typeof(NodeRes))]
    public static string ReplaceRegEx(string input, string pattern, string replacement)
        => Regex.Replace(input, pattern, replacement);

    [Display(Name = "Замена по позиции", Description = "Заменяет символ в указанной позиции", GroupName = "replace", ResourceType = typeof(NodeRes))]
    public static string ReplaceAt(string input, int index, char character)
    {
        if (index < 0 || index >= input.Length) return input;
        char[] chars = input.ToCharArray();
        chars[index] = character;
        return new string(chars);
    }

    [Display(Name = "Замена нескольких символов", Description = "Заменяет несколько символов одновременно", GroupName = "replace", ResourceType = typeof(NodeRes))]
    public static string ReplaceMultiple(string input, string oldChars, string newChars)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(oldChars)) return input;

        var result = new StringBuilder(input);
        for (int i = 0; i < oldChars.Length && i < newChars.Length; i++)
        {
            result.Replace(oldChars[i], newChars[i]);
        }
        return result.ToString();
    }

    // ========== Работа с массивами ==========

    [Display(Name = "Разделить", Description = "Разделяет строку на массив подстрок", GroupName = "array", ResourceType = typeof(NodeRes))]
    public static string[] Split(string input, string separator, StringSplitOptions options = StringSplitOptions.None)
    {
        return input.Split(new[] { separator }, options);
    }

    [Display(Name = "Разделить по нескольким разделителям", Description = "Разделяет строку по нескольким разделителям", GroupName = "array", ResourceType = typeof(NodeRes))]
    public static string[] SplitMultiple(string input, string separators)
    {
        var separatorArray = separators.ToCharArray();
        return input.Split(separatorArray, StringSplitOptions.RemoveEmptyEntries);
    }

    [Display(Name = "Объединить", Description = "Объединяет массив строк в одну строку", GroupName = "array", ResourceType = typeof(NodeRes))]
    public static string Join(string[] input, string separator)
        => string.Join(separator, input);

    [Display(Name = "Обратный порядок слов", Description = "Разделяет, переворачивает и объединяет", GroupName = "array", ResourceType = typeof(NodeRes))]
    public static string ReverseWords(string input, string separator = " ")
    {
        var words = input.Split(new[] { separator }, StringSplitOptions.None);
        Array.Reverse(words);
        return string.Join(separator, words);
    }

    // ========== Проверки и валидация ==========

    [Display(Name = "Начинается с", Description = "Проверяет, начинается ли строка с указанной подстроки", GroupName = "check", ResourceType = typeof(NodeRes))]
    public static string StartsWith(string input, string value, bool ignoreCase = false)
    {
        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return input.StartsWith(value, comparison).ToString();
    }

    [Display(Name = "Заканчивается на", Description = "Проверяет, заканчивается ли строка на указанную подстроку", GroupName = "check", ResourceType = typeof(NodeRes))]
    public static string EndsWith(string input, string value, bool ignoreCase = false)
    {
        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return input.EndsWith(value, comparison).ToString();
    }

    [Display(Name = "Содержит", Description = "Проверяет, содержит ли строка подстроку", GroupName = "check", ResourceType = typeof(NodeRes))]
    public static string Contains(string input, string value, bool ignoreCase = false)
    {
        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return input.Contains(value, comparison).ToString();
    }

    [Display(Name = "Пустая или null", Description = "Проверяет, является ли строка null или пустой", GroupName = "check", ResourceType = typeof(NodeRes))]
    public static string IsNullOrEmpty(string input) => string.IsNullOrEmpty(input).ToString();

    [Display(Name = "Пустая или пробелы", Description = "Проверяет, является ли строка null, пустой или состоящей из пробелов", GroupName = "check", ResourceType = typeof(NodeRes))]
    public static string IsNullOrWhiteSpace(string input) => string.IsNullOrWhiteSpace(input).ToString();

    [Display(Name = "Только буквы", Description = "Проверяет, состоит ли строка только из букв", GroupName = "check", ResourceType = typeof(NodeRes))]
    public static string IsAlpha(string input) => input.All(char.IsLetter).ToString();

    [Display(Name = "Только цифры", Description = "Проверяет, состоит ли строка только из цифр", GroupName = "check", ResourceType = typeof(NodeRes))]
    public static string IsNumeric(string input) => input.All(char.IsDigit).ToString();

    [Display(Name = "Буквы и цифры", Description = "Проверяет, состоит ли строка из букв и цифр", GroupName = "check", ResourceType = typeof(NodeRes))]
    public static string IsAlphaNumeric(string input) => input.All(char.IsLetterOrDigit).ToString();

    [Display(Name = "Email", Description = "Проверяет, является ли строка корректным email", GroupName = "check", ResourceType = typeof(NodeRes))]
    public static string IsEmail(string input)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(input);
            return (addr.Address == input).ToString();
        }
        catch
        {
            return false.ToString();
        }
    }

    // ========== Преобразования ==========

    [Display(Name = "В CamelCase", Description = "Преобразует в camelCase (первая буква строчная)", GroupName = "transform", ResourceType = typeof(NodeRes))]
    public static string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToLower(input[0]) + input.Substring(1);
    }

    [Display(Name = "В PascalCase", Description = "Преобразует в PascalCase (первая буква заглавная)", GroupName = "transform", ResourceType = typeof(NodeRes))]
    public static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpper(input[0]) + input.Substring(1);
    }

    [Display(Name = "В snake_case", Description = "Преобразует в snake_case", GroupName = "transform", ResourceType = typeof(NodeRes))]
    public static string ToSnakeCase(string input)
    {
        return Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2").ToLower();
    }

    [Display(Name = "В kebab-case", Description = "Преобразует в kebab-case", GroupName = "transform", ResourceType = typeof(NodeRes))]
    public static string ToKebabCase(string input)
    {
        return Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1-$2").ToLower();
    }

    [Display(Name = "Обратный порядок", Description = "Переворачивает строку", GroupName = "transform", ResourceType = typeof(NodeRes))]
    public static string Reverse(string input)
    {
        char[] chars = input.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    [Display(Name = "Форматирование", Description = "Форматирует строку с аргументами", GroupName = "transform", ResourceType = typeof(NodeRes))]
    public static string Format(string input, object arg1, object arg2)
        => string.Format(input, [arg1, arg2]);

    [Display(Name = "Повторить строку", Description = "Повторяет строку указанное количество раз (например, 'abc' × 3 → 'abcabcabc')", GroupName = "transform", ResourceType = typeof(NodeRes))]
    public static string Repeat(string input, int count)
    {
        if (string.IsNullOrEmpty(input)) return input;
        if (count <= 0) return string.Empty;

        return string.Concat(Enumerable.Repeat(input, count));
    }

    // ========== Кодирование ==========

    [Display(Name = "Encode/Decode", Description = "Изменяет кодировку строки", GroupName = "encode", ResourceType = typeof(NodeRes))]
    public static string Encode(string input, string sourceEncode, string destEncode)
        => Encoding.GetEncoding(sourceEncode).GetString(Encoding.GetEncoding(destEncode).GetBytes(input));

    [Display(Name = "URL Encode", Description = "Кодирует строку для URL", GroupName = "encode", ResourceType = typeof(NodeRes))]
    public static string UrlEncode(string input) => Uri.EscapeDataString(input);

    [Display(Name = "URL Decode", Description = "Декодирует URL-строку", GroupName = "encode", ResourceType = typeof(NodeRes))]
    public static string UrlDecode(string input) => Uri.UnescapeDataString(input);

    [Display(Name = "HTML Encode", Description = "Кодирует строку для HTML", GroupName = "encode", ResourceType = typeof(NodeRes))]
    public static string HtmlEncode(string input) => System.Net.WebUtility.HtmlEncode(input);

    [Display(Name = "HTML Decode", Description = "Декодирует HTML-строку", GroupName = "encode", ResourceType = typeof(NodeRes))]
    public static string HtmlDecode(string input) => System.Net.WebUtility.HtmlDecode(input);

    [Display(Name = "Base64 Encode", Description = "Кодирует строку в Base64", GroupName = "encode", ResourceType = typeof(NodeRes))]
    public static string Base64Encode(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(bytes);
    }

    [Display(Name = "Base64 Decode", Description = "Декодирует строку из Base64", GroupName = "encode", ResourceType = typeof(NodeRes))]
    public static string Base64Decode(string input)
    {
        var bytes = Convert.FromBase64String(input);
        return Encoding.UTF8.GetString(bytes);
    }

    // ========== Статистика ==========

    [Display(Name = "Длина", Description = "Возвращает длину строки", GroupName = "stats", ResourceType = typeof(NodeRes))]
    public static string Length(string input) => input.Length.ToString();

    [Display(Name = "Количество слов", Description = "Подсчитывает количество слов", GroupName = "stats", ResourceType = typeof(NodeRes))]
    public static string WordCount(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "0";
        return Regex.Matches(input, @"\S+").Count.ToString();
    }

    [Display(Name = "Количество символов без пробелов", Description = "Подсчитывает количество символов без учета пробелов", GroupName = "stats", ResourceType = typeof(NodeRes))]
    public static string CharacterCountWithoutSpaces(string input)
    {
        return input.Count(c => !char.IsWhiteSpace(c)).ToString();
    }

    [Display(Name = "Количество вхождений", Description = "Подсчитывает количество вхождений подстроки", GroupName = "stats", ResourceType = typeof(NodeRes))]
    public static string CountOccurrences(string input, string value, bool ignoreCase = false)
    {
        if (string.IsNullOrEmpty(value)) return "0";

        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        int count = 0, index = 0;

        while ((index = input.IndexOf(value, index, comparison)) != -1)
        {
            count++;
            index += value.Length;
        }

        return count.ToString();
    }
}
