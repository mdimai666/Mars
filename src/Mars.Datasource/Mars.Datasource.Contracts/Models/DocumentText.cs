namespace Mars.Datasource.Contracts.Models;

/// <summary>Блок документа запросов: границы строк (1-based, включительно) и его текст.</summary>
public record DocumentBlock(int StartLine, int EndLine, string Text);

/// <summary>
/// Документ запросов <c>.http</c> как текст. В редакторе он лежит целиком («плашмя» все эндпоинты),
/// поэтому дерево переходит к нужному блоку, а «выполнить» берёт блок под курсором — эти операции
/// над строками живут здесь, чтобы ими пользовались и фронт, и сервер.
/// </summary>
public static partial class DocumentText
{
    const string Separator = "###";
    const string NameDirective = "@name";

    /// <summary>Разделяет документ на блоки: блок начинается с <c>###</c> (или с начала файла).</summary>
    public static List<DocumentBlock> Blocks(string? text)
    {
        List<DocumentBlock> blocks = [];

        if (string.IsNullOrWhiteSpace(text)) return blocks;

        var lines = Lines(text);

        var start = 0;
        var found = false;

        for (var index = 0; index < lines.Length; index++)
        {
            if (!IsSeparator(lines[index])) continue;

            // Первый блок начинается с разделителя; текст до него (переменные документа) в блоки не входит
            Add(blocks, lines, found ? start : index, index);

            start = index;
            found = true;
        }

        Add(blocks, lines, start, lines.Length);

        return blocks;
    }

    /// <summary>Блок, в который попадает строка (курсор редактора); null — строка вне блоков.</summary>
    public static DocumentBlock? BlockAt(string? text, int line)
        => Blocks(text).FirstOrDefault(block => line >= block.StartLine && line <= block.EndLine);

    /// <summary>Блок, чей текст содержит искомую строку (например, заготовку операции из каталога).</summary>
    public static DocumentBlock? BlockContaining(string? text, string fragment)
    {
        if (string.IsNullOrWhiteSpace(fragment)) return null;

        return Blocks(text).FirstOrDefault(block => block.Text.Contains(fragment, StringComparison.Ordinal));
    }

    /// <summary>Дописать блок в конец документа.</summary>
    public static string Append(string? text, string block)
    {
        var body = (text ?? "").TrimEnd();
        var addition = (block ?? "").Trim();

        if (addition.Length == 0) return body;

        return body.Length == 0 ? addition + "\n" : body + "\n\n" + Separator + "\n" + addition + "\n";
    }

    /// <summary>
    /// Скопировать блок сразу после него самого. Имени даём следующий свободный номер
    /// (<c># @name posts</c> → <c># @name posts (2)</c>), иначе в дереве было бы два одинаковых запроса.
    /// </summary>
    public static string Duplicate(string? text, DocumentBlock block)
    {
        if (string.IsNullOrWhiteSpace(text)) return text ?? "";

        var lines = Lines(text).ToList();
        var copy = Lines(Rename(block.Text, NextName(text, BlockName(block.Text))));

        // У блока с разделителем он уже в тексте — второй не нужен
        var insertion = copy.Length > 0 && IsSeparator(copy[0]) ? copy.ToList() : [Separator, .. copy];

        lines.InsertRange(block.EndLine, insertion);

        return string.Join('\n', lines) + "\n";
    }

    /// <summary>Убрать блок из документа вместе с его разделителем.</summary>
    public static string Remove(string? text, DocumentBlock block)
    {
        if (string.IsNullOrWhiteSpace(text)) return text ?? "";

        var lines = Lines(text).ToList();

        var start = block.StartLine - 1;
        var count = Math.Min(block.EndLine, lines.Count) - start;

        if (count <= 0) return text!;

        lines.RemoveRange(start, count);

        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0])) lines.RemoveAt(0);
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1])) lines.RemoveAt(lines.Count - 1);

        return lines.Count == 0 ? "" : string.Join('\n', lines) + "\n";
    }

    /// <summary>Имя блока из <c># @name</c> или подписи разделителя; пусто — блок без имени.</summary>
    public static string BlockName(string? blockText)
    {
        foreach (var line in Lines(blockText ?? ""))
        {
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith(Separator, StringComparison.Ordinal))
            {
                var label = trimmed[Separator.Length..].Trim();

                if (label.Length > 0) return label;

                continue;
            }

            var rest = trimmed.TrimStart('#', ' ', '\t');

            if (rest.StartsWith(NameDirective, StringComparison.OrdinalIgnoreCase))
            {
                return rest[NameDirective.Length..].Trim();
            }
        }

        return "";
    }

    static string Rename(string blockText, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return blockText;

        var lines = Lines(blockText);

        for (var index = 0; index < lines.Length; index++)
        {
            var trimmed = lines[index].TrimStart();
            var rest = trimmed.TrimStart('#', ' ', '\t');

            if (!rest.StartsWith(NameDirective, StringComparison.OrdinalIgnoreCase)) continue;

            var indent = lines[index][..(lines[index].Length - lines[index].TrimStart().Length)];

            if (trimmed.StartsWith(Separator, StringComparison.Ordinal))
            {
                lines[index] = Separator + " " + name;
                continue;
            }

            lines[index] = $"{indent}# {NameDirective} {name}";
        }

        return string.Join('\n', lines);
    }

    /// <summary>Свободное имя для копии: <c>posts</c> → <c>posts (2)</c>, <c>posts (2)</c> → <c>posts (3)</c>.</summary>
    static string NextName(string text, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";

        var baseName = CopySuffix().Replace(name, "");
        var taken = Blocks(text).Select(block => BlockName(block.Text)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var index = 2; ; index++)
        {
            var candidate = $"{baseName} ({index})";

            if (!taken.Contains(candidate)) return candidate;
        }
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"\s+\(\d+\)$")]
    private static partial System.Text.RegularExpressions.Regex CopySuffix();

    static void Add(List<DocumentBlock> blocks, string[] lines, int start, int end)
    {
        if (start >= end) return;

        var text = string.Join('\n', lines.Skip(start).Take(end - start)).TrimEnd();

        if (text.Length == 0) return;

        // Границы — по значимым строкам: пустая строка перед следующим разделителем не часть блока
        var lastLine = start + text.Split('\n').Length;

        blocks.Add(new DocumentBlock(start + 1, lastLine, text));
    }

    static bool IsSeparator(string line) => line.TrimStart().StartsWith(Separator, StringComparison.Ordinal);

    static string[] Lines(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
}
