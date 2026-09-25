namespace Mars.Datasource.Contracts.Sql;

/// <summary>Итог сборки DDL: либо готовый SQL для `NonQuery`, либо текст ошибки для пользователя.</summary>
public record ViewDdlResult
{
    public string? Sql { get; init; }
    public string? Error { get; init; }

    public bool Ok => Sql is not null;

    public static ViewDdlResult Fail(string error) => new() { Error = error };

    public static ViewDdlResult Done(string sql) => new() { Sql = sql };
}

/// <summary>
/// Сборка DDL вьюхи: `CREATE [OR REPLACE|OR ALTER] VIEW` и `DROP VIEW`.
///
/// Тело проверяется на «ровно один SELECT-запрос». Это не про права (редактор и так принимает любой SQL),
/// а про совпадение предпросмотра с выполнением: команда уходит в базу одним запросом, а Npgsql и
/// SqlClient исполняют несколько инструкций, разделённых `;`, — тело вида `SELECT 1; DROP TABLE x`
/// выполнило бы и второе, показав пользователю только `CREATE VIEW`.
/// </summary>
public static class ViewDdlBuilder
{
    static readonly string[] QueryKeywords = ["SELECT", "WITH", "VALUES", "TABLE"];

    public static ViewDdlResult Create(SqlDialect dialect, string? schemaName, string? viewName, string? body, bool replace)
    {
        if (NameError(viewName) is { } nameError) return ViewDdlResult.Fail(nameError);

        var (error, normalized) = Inspect(dialect, body);
        if (error is not null) return ViewDdlResult.Fail(error);

        var target = SqlDialectMapping.Target(dialect, schemaName, viewName!.Trim());
        var keyword = replace
            ? (dialect == SqlDialect.MsSql ? "CREATE OR ALTER VIEW" : "CREATE OR REPLACE VIEW")
            : "CREATE VIEW";

        return ViewDdlResult.Done($"{keyword} {target} AS\n{normalized}");
    }

    /// <summary>Обычная вьюха: `DROP VIEW`. Удаление зависимых объектов (`CASCADE`) не подставляем.</summary>
    public static ViewDdlResult Drop(SqlDialect dialect, string? schemaName, string? viewName)
    {
        if (NameError(viewName) is { } nameError) return ViewDdlResult.Fail(nameError);

        return ViewDdlResult.Done($"DROP VIEW {SqlDialectMapping.Target(dialect, schemaName, viewName!.Trim())}");
    }

    static string? NameError(string? viewName)
    {
        if (string.IsNullOrWhiteSpace(viewName)) return "Имя вьюхи не задано";

        return viewName.Trim().Contains('.')
            ? "Имя вьюхи — без схемы: схема выбирается отдельно"
            : null;
    }

    /// <summary>Пустая строка ошибки — тело в порядке; иначе возвращает причину и нормализованное тело.</summary>
    static (string? Error, string? Body) Inspect(SqlDialect dialect, string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return ("Тело вьюхи не задано", null);

        var text = body.Trim();

        if (FirstKeyword(text) is not { } keyword) return ("Тело вьюхи — SELECT-запрос", null);

        if (!QueryKeywords.Contains(keyword))
        {
            return ($"Тело вьюхи — SELECT-запрос, а не «{keyword}»", null);
        }

        var lastSemicolon = -1;

        foreach (var semicolon in Semicolons(dialect, text))
        {
            if (!OnlyTriviaAfter(text, semicolon + 1))
            {
                return ($"Тело вьюхи — один запрос: лишняя «;» на позиции {semicolon + 1}", null);
            }

            lastSemicolon = semicolon;
        }

        // Хвостовую «;» (её обычно и приносят копипастом) убираем сами: она закрыла бы инструкцию.
        var normalized = lastSemicolon < 0 ? text : text.Remove(lastSemicolon, 1).TrimEnd();

        return (null, normalized);
    }

    static string? FirstKeyword(string text)
    {
        var index = SkipTrivia(text, 0);
        var start = index;

        while (index < text.Length && (char.IsLetter(text[index]) || text[index] == '_')) index++;

        return index == start ? null : text[start..index].ToUpperInvariant();
    }

    /// <summary>
    /// Позиции «;» вне литералов, идентификаторов и комментариев. Разбор идёт по правилам диалекта:
    /// если считать иначе, чем движок, проверка и парсер разойдутся на экранировании.
    /// </summary>
    static IEnumerable<int> Semicolons(SqlDialect dialect, string text)
    {
        var index = 0;

        while (index < text.Length)
        {
            var c = text[index];

            if (c == '-' && index + 1 < text.Length && text[index + 1] == '-')
            {
                var end = text.IndexOf('\n', index);
                index = end < 0 ? text.Length : end + 1;
                continue;
            }

            if (c == '/' && index + 1 < text.Length && text[index + 1] == '*')
            {
                index = SkipBlockComment(text, index);
                continue;
            }

            if (c is '\'' or '"' or '`')
            {
                // Обратный слэш экранирует кавычку только в MySQL (Postgres: standard_conforming_strings).
                index = SkipQuoted(text, index, c, c == '\'' && dialect == SqlDialect.MySql);
                continue;
            }

            if (c == '$' && DollarQuoteTag(text, index) is { } tag)
            {
                var end = text.IndexOf(tag, index + tag.Length, StringComparison.Ordinal);
                index = end < 0 ? text.Length : end + tag.Length;
                continue;
            }

            if (c == ';') yield return index;

            index++;
        }
    }

    static int SkipQuoted(string text, int index, char quote, bool backslashEscapes)
    {
        index++;

        while (index < text.Length)
        {
            if (backslashEscapes && text[index] == '\\' && index + 1 < text.Length)
            {
                index += 2;
                continue;
            }

            if (text[index] == quote)
            {
                // Удвоенная кавычка — экранированная, литерал продолжается.
                if (index + 1 < text.Length && text[index + 1] == quote)
                {
                    index += 2;
                    continue;
                }

                return index + 1;
            }

            index++;
        }

        return index;
    }

    /// <summary>Тег dollar-quoting (`$$` или `$tag$`) — иначе `$1$` это параметр, а не литерал.</summary>
    static string? DollarQuoteTag(string text, int index)
    {
        var end = index + 1;

        while (end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] == '_')) end++;

        if (end >= text.Length || text[end] != '$') return null;
        if (end > index + 1 && char.IsDigit(text[index + 1])) return null;

        return text[index..(end + 1)];
    }

    /// <summary>Пропускает пробелы и комментарии; блочные считает вложенными, как Postgres.</summary>
    static int SkipTrivia(string text, int index)
    {
        while (index < text.Length)
        {
            if (char.IsWhiteSpace(text[index]))
            {
                index++;
                continue;
            }

            if (index + 1 < text.Length && text[index] == '-' && text[index + 1] == '-')
            {
                var end = text.IndexOf('\n', index);
                index = end < 0 ? text.Length : end + 1;
                continue;
            }

            if (index + 1 < text.Length && text[index] == '/' && text[index + 1] == '*')
            {
                index = SkipBlockComment(text, index);
                continue;
            }

            break;
        }

        return index;
    }

    static bool OnlyTriviaAfter(string text, int index) => SkipTrivia(text, index) >= text.Length;

    static int SkipBlockComment(string text, int index)
    {
        var depth = 0;

        while (index < text.Length)
        {
            if (index + 1 < text.Length && text[index] == '/' && text[index + 1] == '*')
            {
                depth++;
                index += 2;
                continue;
            }

            if (index + 1 < text.Length && text[index] == '*' && text[index + 1] == '/')
            {
                depth--;
                index += 2;

                if (depth == 0) return index;
                continue;
            }

            index++;
        }

        return index;
    }
}
