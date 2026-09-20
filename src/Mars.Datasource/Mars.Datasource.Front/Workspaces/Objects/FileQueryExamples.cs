namespace Mars.Datasource.Front.Workspaces.Objects;

/// <summary>Заготовка условия фильтра для запроса к файлу из меню «примеры».</summary>
public record FileQueryExample(string Name, string Text);

/// <summary>
/// Примеры условий Dynamic LINQ: вставляются в редактор заготовкой, которую пользователь
/// правит под колонки своего файла. Строки файла — текст, поэтому сравнение «как число»
/// или «как дата» делается через приведения <c>Val.*</c>.
/// </summary>
public static class FileQueryExamples
{
    public static IReadOnlyList<FileQueryExample> All { get; } =
    [
        new("Число — сравнение",
            "Val.Num(age) > 30"),

        new("Текст — точное совпадение",
            "name == \"ann\""),

        new("Текст — без учёта регистра",
            "Val.Str(name).ToLower() == \"ann\""),

        new("Текст — по началу строки",
            "Val.Str(name).StartsWith(\"a\")"),

        new("Текст — содержит подстроку",
            "Val.Str(description).Contains(\"error\")"),

        new("Дата — позже указанной",
            "Val.Date(created) > Val.Date(\"2020-01-01\")"),

        new("Флаг — истина",
            "Val.Flag(active) == true"),

        new("Несколько условий",
            "Val.Num(price) >= 100 && Val.Str(category) == \"tools\""),
    ];
}
