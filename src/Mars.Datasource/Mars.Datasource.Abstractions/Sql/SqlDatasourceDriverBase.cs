using System.Data.Common;
using System.Diagnostics;
using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.Datasource.Contracts.Sql;

namespace Mars.Datasource.Abstractions.Sql;

/// <summary>
/// Общая ADO-реализация sql-драйвера. Движок даёт соединение, команду и тексты системных каталогов;
/// выполнение запроса, параметры, лимит строк и сборка структуры одинаковы для всех движков.
/// Порядок колонок выборки метаданных — см. <see cref="QDatabaseStructureBuilder"/>.
/// </summary>
public abstract class SqlDatasourceDriverBase : IDatasourceDriver
{
    protected DatasourceConfig Config { get; }

    /// <summary>База из строки подключения: параметр отбора каталога у движков, которые фильтруют по базе.</summary>
    protected string Database { get; }

    protected SqlDatasourceDriverBase(DatasourceConfig config)
    {
        Config = config;
        Database = config.GetDatabaseName();
    }

    protected abstract DbConnection CreateConnection();

    protected abstract DbCommand CreateCommand(string sql, DbConnection connection);

    /// <summary>Таблицы и вьюхи одним запросом: schema_name, table_name, table_owner, kind.</summary>
    protected abstract string TablesSql { get; }

    /// <summary>Колонки одним запросом: schema, table, column, ordinal, type, size, is_nullable, is_key.</summary>
    protected abstract string ColumnsSql { get; }

    protected abstract string ColumnsOrderBySql { get; }

    protected abstract string ViewDefinitionSql { get; }

    /// <summary>Префикс имени параметра: Npgsql принимает имена и без «@».</summary>
    protected virtual string ParameterPrefix => "@";

    /// <summary>Дополнительный отбор колонок одной таблицы; добавляется к <see cref="ColumnsSql"/>.</summary>
    protected virtual string ColumnsTableFilterSql => " AND c.TABLE_NAME = @tableName";

    /// <summary>Каталог отбирается по базе параметром (у Postgres отбор схем внутри самого SQL).</summary>
    protected virtual bool UsesDatabaseParameter => true;

    /// <summary>Хук типа параметра: Postgres отправляет строки как unknown, чтобы тип вывела сама база.</summary>
    protected virtual Action<DbParameter>? ConfigureParameter => null;

    /// <summary>Поле результата: движок знает из схемы больше, чем базовый <see cref="DbColumn"/>.</summary>
    protected virtual DatasourceField MapField(DbColumn column) => AdoResultReader.Field(column);

    /// <summary>Утилиты движка: по умолчанию их нет, свои объявляет движок (полезные запросы PostgreSQL).</summary>
    public virtual IReadOnlyList<DatasourceActionDescriptor> Actions => [];

    public virtual Task<QueryResultDto> ExecuteAction(string actionId, CancellationToken cancellationToken = default)
        => Task.FromResult(new QueryResultDto
        {
            Ok = false,
            Message = $"Действие \"{actionId}\" не найдено у источника",
            Kind = Config.Kind,
            Driver = Config.Driver,
        });

    public async Task<QueryResultDto> Query(DatasourceRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        QueryResultDto result = new()
        {
            Kind = Config.Kind,
            Driver = Config.Driver,
            Command = request.Query,
        };

        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = CreateCommand(request.Query, connection);
            AdoResultReader.ApplyParameters(command, request.Parameters, ConfigureParameter);

            if (request.TimeoutSec is int timeoutSec)
            {
                command.CommandTimeout = timeoutSec;
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var columns = await reader.GetColumnSchemaAsync(cancellationToken);
            result.Fields = columns.Select(MapField).ToArray();

            (result.Rows, result.Truncated) = await AdoResultReader.ReadRowsAsync(reader, request.MaxRows, cancellationToken);
            result.Ok = true;
            result.Message = "success";
        }
        catch (Exception ex)
        {
            result.Ok = false;
            result.Message = QueryResultMapping.Error(ex);
        }

        stopwatch.Stop();
        result.ElapsedMs = stopwatch.ElapsedMilliseconds;

        return result;
    }

    public async Task<DatasourceModifyResult> NonQuery(string sql, IReadOnlyList<DatasourceParam>? parameters = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = CreateCommand(sql, connection);
            AdoResultReader.ApplyParameters(command, parameters, ConfigureParameter);

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

            return new DatasourceModifyResult
            {
                Ok = true,
                Message = "success",
                Kind = Config.Kind,
                Driver = Config.Driver,
                RowsAffected = rowsAffected,
            };
        }
        catch (Exception ex)
        {
            return new DatasourceModifyResult
            {
                Ok = false,
                Message = QueryResultMapping.Error(ex),
                Kind = Config.Kind,
                Driver = Config.Driver,
            };
        }
    }

    public async Task<QDatabaseStructure> DatabaseStructure()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        return QDatabaseStructureBuilder.Assemble(
            connection.Database,
            await ReadTablesAsync(connection),
            await ReadColumnsAsync(connection, null));
    }

    public async Task<List<QTableSchema>> Tables()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var metas = await ReadTablesAsync(connection);

        return metas.Select(QDatabaseStructureBuilder.ToSchema).ToList();
    }

    public async Task<Dictionary<string, QTableColumn>> Columns(string tableName)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var metas = await ReadColumnsAsync(connection, tableName);

        return metas.ToDictionary(meta => meta.ColumnName, QDatabaseStructureBuilder.ToColumn);
    }

    public async Task<string?> ViewDefinition(string schemaName, string tableName)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var command = CreateCommand(ViewDefinitionSql, connection);
        AddParameter(command, "schemaName", schemaName ?? "");
        AddParameter(command, "tableName", tableName);

        return await command.ExecuteScalarAsync() as string;
    }

    protected async Task<List<QTableMeta>> ReadTablesAsync(DbConnection connection)
    {
        await using var command = CreateCommand(TablesSql, connection);

        if (UsesDatabaseParameter) AddParameter(command, "database", Database);

        return await ReadMetasAsync(command, QDatabaseStructureBuilder.ReadTable);
    }

    protected async Task<List<QColumnMeta>> ReadColumnsAsync(DbConnection connection, string? tableName)
    {
        var sql = tableName is null
            ? ColumnsSql + ColumnsOrderBySql
            : ColumnsSql + ColumnsTableFilterSql + ColumnsOrderBySql;

        await using var command = CreateCommand(sql, connection);

        if (UsesDatabaseParameter) AddParameter(command, "database", Database);

        if (tableName is not null) AddParameter(command, "tableName", tableName);

        return await ReadMetasAsync(command, QDatabaseStructureBuilder.ReadColumn);
    }

    static async Task<List<TMeta>> ReadMetasAsync<TMeta>(DbCommand command, Func<DbDataReader, TMeta> read)
    {
        await using var reader = await command.ExecuteReaderAsync();

        List<TMeta> list = [];

        while (await reader.ReadAsync())
        {
            list.Add(read(reader));
        }

        return list;
    }

    protected void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = ParameterPrefix + name;
        parameter.Value = value ?? DBNull.Value;

        command.Parameters.Add(parameter);
    }
}
