namespace Mars.Contracts.NavMenus;

public record NavMenuSummaryResponse
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required bool Disabled { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
}

public record NavMenuDetailResponse
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset? ModifiedAt { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required bool Disabled { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required IReadOnlyCollection<NavMenuItemResponse> MenuItems { get; init; }
    public required string Class { get; init; }
    public required string Style { get; init; }
    public required IReadOnlyCollection<string> Roles { get; init; }
    public required bool RolesInverse { get; init; }

    /// <summary>
    /// false — меню не сохранено в БД и отдаётся из дефолтного (генерируемого кодом) состояния.
    /// </summary>
    public required bool IsPersisted { get; init; }
}

public record NavMenuItemResponse
{
    public required Guid Id { get; init; }
    public required Guid ParentId { get; init; }
    public required string Title { get; init; }
    public required string Url { get; init; }
    public required string? Icon { get; init; }
    public required IReadOnlyCollection<string> Roles { get; init; }
    public required bool RolesInverse { get; init; }
    public required string Class { get; init; }
    public required string Style { get; init; }
    public required bool OpenInNewTab { get; init; }
    public required bool Disabled { get; init; }
    public required bool IsHeader { get; init; }
    public required bool IsDivider { get; init; }

    /// <summary>
    /// Пункт из дефолтного (генерируемого кодом) системного меню:
    /// его нельзя удалить, только скрыть (Disabled).
    /// </summary>
    public required bool IsSystem { get; init; }
}
