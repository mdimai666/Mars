namespace Mars.Datasource.Abstractions.Services;

/// <summary>
/// Операции sql-источника, которых нет у других типов источников. Отдельный контракт:
/// общий <see cref="IDatasourceService"/> остаётся kind-независимым.
/// </summary>
public interface ISqlDatasourceService
{
    /// <summary>Определение вьюхи (текст запроса), null — объекта нет или это не вьюха.</summary>
    Task<string?> ViewDefinition(string slug, string? schemaName, string tableName);
}
