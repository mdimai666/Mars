namespace Mars.Shared.Contracts.PostTypes;

/// <summary>
/// Настройки грида постов типа в админке: порядок и видимость колонок, сортировка по умолчанию.
/// Ключи колонок — базовые колонки (<see cref="PostTypeGridConstants"/>) или ключи мета-полей типа.
/// Колонки, отсутствующие в списке (например, добавленные позже), грид показывает в конце.
/// </summary>
public record PostTypeGridSettings
{
    public IReadOnlyCollection<PostTypeGridColumn> Columns { get; init; } = [];

    /// <summary>Ключ колонки сортировки по умолчанию; пусто — дефолтная сортировка грида</summary>
    public string? SortKey { get; init; }

    public bool SortDescending { get; init; }
}

public record PostTypeGridColumn
{
    public required string Key { get; init; }
    public bool Visible { get; init; } = true;
}

/// <summary>Ключи базовых (не мета-) колонок грида постов</summary>
public static class PostTypeGridConstants
{
    public const string Title = "title";
    public const string Categories = "categories";
    public const string Status = "status";
    public const string Author = "author";
    public const string CreatedAt = "created_at";

    public static readonly IReadOnlyList<string> BaseColumns = [Title, Categories, Status, Author, CreatedAt];
}
