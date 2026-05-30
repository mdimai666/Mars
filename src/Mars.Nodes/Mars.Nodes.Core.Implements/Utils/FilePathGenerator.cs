using System.Text.RegularExpressions;
using Mars.Core.Features;
using Microsoft.AspNetCore.Http;

namespace Mars.Nodes.Core.Implements.Utils;

/// <summary>
/// Компонент предназначен для динамической генерации путей.
/// </summary>
public static class FilePathGenerator
{
    public static string Generate(string template, IFormFile file, string fieldName)
    {
        if (string.IsNullOrEmpty(template))
            throw new ArgumentNullException(nameof(template));

        var now = DateTime.UtcNow;

        // Получаем компоненты имени файла
        string rawExt = Path.GetExtension(file.FileName); // Напр: ".jpg"
        string rawNameOnly = Path.GetFileNameWithoutExtension(file.FileName); // Напр: "photo"
        string rawFullName = file.FileName; // Напр: "photo.jpg"

        // Безопасная очистка от запрещенных символов
        string safeFileNameOnly = CleanFileName(rawNameOnly);
        string safeFileExt = CleanFileName(rawExt).ToLowerInvariant(); // Расширение всегда в нижнем регистре
        string safeFullName = CleanFileName(rawFullName);
        string safeFieldName = CleanFileName(fieldName);

        var uniqueSuffix = TextTool.GenerateUniqueSuffix();
        var uniqueFileName = $"{safeFileNameOnly}_{uniqueSuffix}{safeFileExt}";

        // Расширенный словарь плейсхолдеров
        var tokens = new Dictionary<string, string>()
        {
            // Имена и поля
            { "{file_name}", safeFullName },          // Имя с расширением (photo.jpg)
            { "{file_name_only}", safeFileNameOnly }, // Только имя без расширения (photo)
            { "{file_ext}", safeFileExt },            // Только расширение с точкой (.jpg)
            { "{field_name}", safeFieldName },        // Имя поля из формы
            { "{guid}", Guid.NewGuid().ToString("N") },// Уникальный ID (32 символа)
            { "{unique_suffix}", uniqueSuffix },         // Unique Suffix
            { "{unique_file_name}", uniqueFileName },     // Unique FileName

            // Форматы Даты (Год, Месяц, День)
            { "{yyyy}", now.ToString("yyyy") },       // 2026
            { "{yy}", now.ToString("yy") },           // 26
            { "{MM}", now.ToString("MM") },           // 05 (с ведущим нулем)
            { "{M}", now.ToString("%M") },            // 5 (без ведущего нуля)
            { "{DD}", now.ToString("dd") },           // 09 (с ведущим нулем)
            { "{D}", now.ToString("%d") },            // 9 (без ведущего нуля)

            // Форматы Времени (Часы, Минуты, Секунды)
            { "{HH}", now.ToString("HH") },           // 14 (24-часовой, с нулем)
            { "{H}", now.ToString("%H") },            // 14 (без нуля)
            { "{mm}", now.ToString("mm") },           // 05 (минуты)
            { "{ss}", now.ToString("ss") }            // 34 (секунды)
        };

        string resultPath = template;
        foreach (var token in tokens)
        {
            resultPath = resultPath.Replace(token.Key, token.Value);
        }

        // Нормализация слэшей под текущую ОС (Windows: \, Linux/macOS: /)
        resultPath = resultPath.Replace('\\', Path.DirectorySeparatorChar)
                               .Replace('/', Path.DirectorySeparatorChar);

        return resultPath;
    }

    private static string CleanFileName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "file";

        // Удаляем системные символы путей и запрещенные ОС знаки
        var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()) + "/\\");
        string cleaned = Regex.Replace(name, $"[{invalidChars}]", "_").Replace("..", "_");

        return cleaned.Trim('_');
    }
}
