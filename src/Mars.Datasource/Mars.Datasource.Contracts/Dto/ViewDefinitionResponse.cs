namespace Mars.Datasource.Contracts.Dto;

/// <summary>
/// Определение вьюхи. Пустой `Sql` означает, что движок текст не отдал —
/// объект не вьюха или уже удалён.
/// </summary>
public record ViewDefinitionResponse
{
    public required string Sql { get; init; }
}
