using System.Collections;
using Mars.Nodes.Core.StringFunctions;

namespace Mars.Nodes.Implements.Test.Nodes;

public class StringNodeOperationTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        // ========== Базовые преобразования ==========
        yield return new object[] { nameof(StringNodeOperationUtils.ToUpper), "text", "TEXT", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.ToLower), "TEXT", "text", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.Capitalize), "hello world", "Hello world", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.Capitalize), "HELLO", "Hello", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.ToTitleCase), "hello world test", "Hello World Test", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.InvertCase), "Hello World", "hELLO wORLD", null! };

        // ========== Добавление строк ==========
        yield return new object[] { nameof(StringNodeOperationUtils.Prepend), "world", "Hello world", new object[] { "Hello " } };
        yield return new object[] { nameof(StringNodeOperationUtils.Append), "Hello", "Hello world", new object[] { " world" } };
        yield return new object[] { nameof(StringNodeOperationUtils.PrependIfNotExists), "world", "Hello world", new object[] { "Hello ", false } };
        yield return new object[] { nameof(StringNodeOperationUtils.PrependIfNotExists), "Hello world", "Hello world", new object[] { "Hello ", false } };
        yield return new object[] { nameof(StringNodeOperationUtils.AppendIfNotExists), "Hello", "Hello world", new object[] { " world", false } };
        yield return new object[] { nameof(StringNodeOperationUtils.AppendIfNotExists), "Hello world", "Hello world", new object[] { " world", false } };
        yield return new object[] { nameof(StringNodeOperationUtils.InsertAt), "Hello world", "Hello beautiful world", new object[] { 6, "beautiful " } };
        yield return new object[] { nameof(StringNodeOperationUtils.InsertAt), "world", "Hello world", new object[] { 0, "Hello " } };
        yield return new object[] { nameof(StringNodeOperationUtils.PadLeft), "123", "  123", new object[] { 5, ' ' } };
        yield return new object[] { nameof(StringNodeOperationUtils.PadLeft), "123", "00123", new object[] { 5, '0' } };
        yield return new object[] { nameof(StringNodeOperationUtils.PadRight), "123", "123  ", new object[] { 5, ' ' } };
        yield return new object[] { nameof(StringNodeOperationUtils.PadRight), "123", "12300", new object[] { 5, '0' } };

        // ========== Удаление и обрезка ==========
        yield return new object[] { nameof(StringNodeOperationUtils.Trim), " Trim ", "Trim", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.TrimStart), " Trim ", "Trim ", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.TrimEnd), " Trim ", " Trim", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.RemovePrefix), "Hello world", " world", new object[] { "Hello", false } };
        yield return new object[] { nameof(StringNodeOperationUtils.RemovePrefix), "hello world", " world", new object[] { "Hello", true } };
        yield return new object[] { nameof(StringNodeOperationUtils.RemoveSuffix), "Hello world", "Hello", new object[] { " world", false } };
        yield return new object[] { nameof(StringNodeOperationUtils.RemoveRange), "Hello world", "Helld", new object[] { 4, 6 } };
        yield return new object[] { nameof(StringNodeOperationUtils.RemoveWhitespace), "Hello   World  Test", "HelloWorldTest", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.RemoveDuplicateSpaces), "Hello    World   Test", "Hello World Test", null! };

        // ========== Извлечение и получение частей ==========
        yield return new object[] { nameof(StringNodeOperationUtils.Left), "Hello world", "Hello", new object[] { 5 } };
        yield return new object[] { nameof(StringNodeOperationUtils.Left), "Hello world", "Hello world", new object[] { 20 } };
        yield return new object[] { nameof(StringNodeOperationUtils.Right), "Hello world", "world", new object[] { 5 } };
        yield return new object[] { nameof(StringNodeOperationUtils.Right), "Hello world", "Hello world", new object[] { 20 } };
        yield return new object[] { nameof(StringNodeOperationUtils.Substring), "Hello world", "world", new object[] { 6, null! } };
        yield return new object[] { nameof(StringNodeOperationUtils.Substring), "Hello world", "wo", new object[] { 6, 2 } };
        yield return new object[] { nameof(StringNodeOperationUtils.Between), "Hello [world] test", "world", new object[] { "[", "]" } };
        yield return new object[] { nameof(StringNodeOperationUtils.Before), "Hello world test", "Hello", new object[] { " world" } };
        yield return new object[] { nameof(StringNodeOperationUtils.After), "Hello world test", "world test", new object[] { "Hello " } };
        yield return new object[] { nameof(StringNodeOperationUtils.BeforeLast), "a,b,c,d", "a,b,c", new object[] { "," } };
        yield return new object[] { nameof(StringNodeOperationUtils.AfterLast), "a,b,c,d", "d", new object[] { "," } };

        // ========== Замена ==========
        yield return new object[] { nameof(StringNodeOperationUtils.Replace), "abcABC", "xbcABC", new object[] { "a", "x", false } };
        yield return new object[] { nameof(StringNodeOperationUtils.Replace), "abcABC", "xbcxBC", new object[] { "a", "x", true } };
        yield return new object[] { nameof(StringNodeOperationUtils.ReplaceRegEx), "abcABC-123", "x-123", new object[] { "[a-zA-Z]+", "x" } };
        yield return new object[] { nameof(StringNodeOperationUtils.ReplaceAt), "Hello", "Hollo", new object[] { 1, 'o' } };
        yield return new object[] { nameof(StringNodeOperationUtils.ReplaceMultiple), "Hello World", "HXXXo WorXd", new object[] { "le", "XX" } };

        // ========== Работа с массивами ==========
        yield return new object[] { nameof(StringNodeOperationUtils.Split), "1,2,3", new string[] { "1", "2", "3" }, new object[] { ",", StringSplitOptions.None } };
        yield return new object[] { nameof(StringNodeOperationUtils.Split), "1,,2,3", new string[] { "1", "", "2", "3" }, new object[] { ",", StringSplitOptions.None } };
        yield return new object[] { nameof(StringNodeOperationUtils.Split), "1,,2,3", new string[] { "1", "2", "3" }, new object[] { ",", StringSplitOptions.RemoveEmptyEntries } };
        yield return new object[] { nameof(StringNodeOperationUtils.SplitMultiple), "1,2;3 4", new string[] { "1", "2", "3", "4" }, new object[] { ",; " } };
        yield return new object[] { nameof(StringNodeOperationUtils.Join), new string[] { "1", "2", "3" }, "1,2,3", new object[] { "," } };
        yield return new object[] { nameof(StringNodeOperationUtils.Join), new string[] { "1", "2", "3" }, "1-2-3", new object[] { "-" } };
        yield return new object[] { nameof(StringNodeOperationUtils.ReverseWords), "Hello world test", "test world Hello", new object[] { " " } };
        yield return new object[] { nameof(StringNodeOperationUtils.ReverseWords), "one,two,three", "three,two,one", new object[] { "," } };

        // ========== Проверки и валидация (возвращают bool) ==========
        yield return new object[] { nameof(StringNodeOperationUtils.StartsWith), "Hello world", "True", new object[] { "Hello", false } };
        yield return new object[] { nameof(StringNodeOperationUtils.StartsWith), "Hello world", "False", new object[] { "world", false } };
        yield return new object[] { nameof(StringNodeOperationUtils.StartsWith), "Hello world", "True", new object[] { "hello", true } };
        yield return new object[] { nameof(StringNodeOperationUtils.EndsWith), "Hello world", "True", new object[] { "world", false } };
        yield return new object[] { nameof(StringNodeOperationUtils.EndsWith), "Hello world", "True", new object[] { "World", true } };
        yield return new object[] { nameof(StringNodeOperationUtils.Contains), "Hello world", "True", new object[] { "world", false } };
        yield return new object[] { nameof(StringNodeOperationUtils.Contains), "Hello world", "False", new object[] { "World", false } };
        yield return new object[] { nameof(StringNodeOperationUtils.IsNullOrEmpty), "", "True", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.IsNullOrEmpty), "text", "False", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.IsNullOrWhiteSpace), "   ", "True", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.IsAlpha), "abc", "True", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.IsAlpha), "abc123", "False", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.IsNumeric), "123", "True", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.IsNumeric), "12.3", "False", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.IsAlphaNumeric), "abc123", "True", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.IsAlphaNumeric), "abc 123", "False", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.IsEmail), "test@example.com", "True", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.IsEmail), "invalid-email", "False", null! };

        // ========== Преобразования ==========
        yield return new object[] { nameof(StringNodeOperationUtils.ToCamelCase), "HelloWorld", "helloWorld", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.ToPascalCase), "helloWorld", "HelloWorld", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.ToSnakeCase), "HelloWorldTest", "hello_world_test", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.ToKebabCase), "HelloWorldTest", "hello-world-test", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.Reverse), "Hello", "olleH", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.Format), "x is {0}.{1}", "x is 3.14", new object[] { "3", "14" } };
        yield return new object[] { nameof(StringNodeOperationUtils.Format), "Hello {0}!", "Hello John!", new object[] { "John" } };

        // ========== Кодирование ==========
        yield return new object[] { nameof(StringNodeOperationUtils.Encode), "РџСЂРёРІРµС‚", "Привет", new object[] { "utf-8", "windows-1251" } };
        yield return new object[] { nameof(StringNodeOperationUtils.Encode), "Hello", "Hello", new object[] { "utf-8", "us-ascii" } };
        yield return new object[] { nameof(StringNodeOperationUtils.Encode), "Тест", "РўРµСЃС‚", new object[] { "windows-1251", "utf-8" } };
        yield return new object[] { nameof(StringNodeOperationUtils.UrlEncode), "hello world", "hello%20world", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.UrlDecode), "hello%20world", "hello world", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.HtmlEncode), "<div>text</div>", "&lt;div&gt;text&lt;/div&gt;", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.HtmlDecode), "&lt;div&gt;text&lt;/div&gt;", "<div>text</div>", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.Base64Encode), "Hello World", "SGVsbG8gV29ybGQ=", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.Base64Decode), "SGVsbG8gV29ybGQ=", "Hello World", null! };

        // ========== Статистика (возвращают числа как строки) ==========
        yield return new object[] { nameof(StringNodeOperationUtils.Length), "Hello", "5", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.Length), "", "0", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.WordCount), "Hello world test", "3", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.WordCount), "Hello   world", "2", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.CharacterCountWithoutSpaces), "Hello world", "10", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.CharacterCountWithoutSpaces), "Hello   world", "10", null! };
        yield return new object[] { nameof(StringNodeOperationUtils.CountOccurrences), "ababab", "3", new object[] { "ab", false } };
        yield return new object[] { nameof(StringNodeOperationUtils.CountOccurrences), "AbAbAb", "3", new object[] { "ab", true } };
        yield return new object[] { nameof(StringNodeOperationUtils.CountOccurrences), "AbAbAb", "0", new object[] { "ab", false } };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
