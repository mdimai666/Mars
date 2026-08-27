namespace Mars.Host.Shared.Dto.Files;

public record CreateFolderQuery
{
    public Guid? ParentId { get; init; }
    public required string Name { get; init; }
    public required Guid UserId { get; init; }

    /// <summary>
    /// Физический путь от upload. Заполняется сервисом.
    /// </summary>
    public string? Path { get; init; }
}
