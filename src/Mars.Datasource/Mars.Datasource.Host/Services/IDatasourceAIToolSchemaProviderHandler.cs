using System.Text;
using Mars.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Mars.Datasource.Host.Services;

public interface IDatasourceAIToolSchemaProviderHandler
{
    public Task<string> Handle();
}

internal class DatasourceAIToolSchemaProviderHandler : IDatasourceAIToolSchemaProviderHandler
{
    private readonly MarsDbContext _marsDbContext;

    public DatasourceAIToolSchemaProviderHandler(MarsDbContext marsDbContext)
    {
        _marsDbContext = marsDbContext;
    }

    public Task<string> Handle()
    {
        var model = _marsDbContext.Model;
        var sb = new StringBuilder();

        foreach (var entityType in model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            var schema = entityType.GetSchema();

            if (string.IsNullOrEmpty(schema))
                sb.AppendLine($"Table: {tableName}");
            else
                sb.AppendLine($"Table: {schema}.{tableName}");

            // Колонки
            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.GetColumnName();
                var clrType = property.ClrType.Name;
                var isNullable = property.IsNullable ? "NULL" : "NOT NULL";
                sb.AppendLine($"  Column: {columnName} ({clrType}) {isNullable}");
            }

            // Первичный ключ
            var pk = entityType.FindPrimaryKey();
            if (pk != null)
            {
                var pkCols = string.Join(", ", pk.Properties.Select(p => p.GetColumnName()));
                sb.AppendLine($"  Primary Key: {pkCols}");
            }

            // Внешние ключи
            foreach (var fk in entityType.GetForeignKeys())
            {
                var fkCols = string.Join(", ", fk.Properties.Select(p => p.GetColumnName()));
                var principalTable = fk.PrincipalEntityType.GetTableName();
                var principalSchema = fk.PrincipalEntityType.GetSchema();
                var principalCols = string.Join(", ", fk.PrincipalKey.Properties.Select(p => p.GetColumnName()));
                sb.AppendLine($"  Foreign Key: {fkCols} => {principalSchema}.{principalTable}({principalCols})");
            }

            sb.AppendLine();
        }

        return Task.FromResult(sb.ToString());
    }
}
