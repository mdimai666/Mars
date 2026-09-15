using System.Collections.Concurrent;
using Mars.Contracts.Common;
using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Abstractions.Models;
using Mars.Datasource.Abstractions.Services;
using Mars.Datasource.Contracts.Dto;
using Mars.Datasource.Contracts.Models;
using Mars.Options.Abstractions.Services;
using Microsoft.Extensions.Configuration;

namespace Mars.Datasource.Host.Services;

/// <summary>
/// singletone
/// </summary>
internal class DatasourceService : IDatasourceService
{
    private readonly IOptionService _optionService;
    private readonly IDatabaseBackupService _databaseBackupService;

    /// <summary>Провайдеры по ключу источника: ядро модуля их не создаёт, их регистрирует корень композиции.</summary>
    readonly Dictionary<string, IDatasourceDriverFactory> _drivers;

    string _connectionString;
    DatasourceConfig _defaultConfig;
    DatasourceOption? _optionValue;
    public DatasourceConfig DefaultConfig => _defaultConfig;

    Dictionary<string, DatasourceConfig>? _configsCache;

    /// <summary>Структура базы стоит десятков запросов к каталогу — держим её недолго в памяти.</summary>
    readonly ConcurrentDictionary<string, (QDatabaseStructure Structure, DateTime At)> _structureCache = new(StringComparer.OrdinalIgnoreCase);
    static readonly TimeSpan StructureCacheTtl = TimeSpan.FromSeconds(30);

    /// <summary>Первый оператор, после которого кэш структуры уже неактуален.</summary>
    static readonly string[] DdlKeywords = ["CREATE", "ALTER", "DROP", "REFRESH", "TRUNCATE"];

    Dictionary<string, DatasourceConfig> configs
    {
        get
        {
            AutoUpdateConfig();
            return _configsCache!;
        }
    }

    public DatasourceService(IConfiguration configuration, IOptionService optionService, IDatabaseBackupService databaseBackupService,
        IEnumerable<IDatasourceDriverFactory> drivers)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;

        _defaultConfig = new DatasourceConfig()
        {
            ConnectionString = _connectionString,
            Driver = "psql",
            Title = "default",
            Slug = "default"
        };
        _optionService = optionService;
        _databaseBackupService = databaseBackupService;
        _drivers = drivers.ToDictionary(d => d.Driver, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<DatasourceDriverResponse> Drivers()
        => _drivers.Values
            .Select(d => new DatasourceDriverResponse
            {
                Driver = d.Driver,
                DefaultConnectionString = d.DefaultConnectionString,
                HelpLink = d.HelpLink,
            })
            .OrderBy(d => d.Driver, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public void InvalidateLocalDictCache(DatasourceOption opt)
    {
        _optionValue = opt;

        // Битые и повторяющиеся slug пропускаем: раньше ToDictionary падал и «ломались» все источники сразу.
        Dictionary<string, DatasourceConfig> configs = new(StringComparer.OrdinalIgnoreCase);

        foreach (var config in opt.Configs)
        {
            if (DatasourceConfig.ValidateSlug(config.Slug) is not null) continue;

            configs.TryAdd(config.Slug, config);
        }

        _configsCache = configs;

        // Сменились настройки источников — структура могла измениться.
        _structureCache.Clear();
    }

    void AutoUpdateConfig()
    {
        if (_optionValue is null)
        {
            _optionValue = _optionService.GetOption<DatasourceOption>();
            InvalidateLocalDictCache(_optionValue);
        }

        var freshOptionValue = _optionService.GetOption<DatasourceOption>();

        if (freshOptionValue == _optionValue) return;

        InvalidateLocalDictCache(freshOptionValue);
    }

    DatasourceConfig GetConfig(string slug)
    {
        if (string.Equals(slug, DatasourceConfig.DefaultSlug, StringComparison.OrdinalIgnoreCase))
        {
            return _defaultConfig;
        }

        if (configs.TryGetValue(slug, out var config))
        {
            return config;
        }

        throw new ArgumentException($"Источник данных \"{slug}\" не найден в настройках");
    }

    IDatasourceDriver ResolveEngine(string slug) => ResolveEngine(GetConfig(slug));

    IDatasourceDriver ResolveEngine(DatasourceConfig config)
    {
        if (_drivers.TryGetValue(config.Driver, out var factory)) return factory.Create(config);

        throw new NotSupportedException($"Провайдер источников \"{config.Driver}\" не подключён");
    }

    public async Task<UserActionResult> TestConnection(ConnectionStringTestDto dto)
    {
        var tmpConfig = new DatasourceConfig()
        {
            ConnectionString = dto.ConnectionString,
            Driver = dto.Driver,
            Title = "TestConnection " + dto.Driver,
            Slug = "test_" + dto.Driver,
        };

        try
        {
            var se = ResolveEngine(tmpConfig);
            var tables = await se.Tables();
            return new UserActionResult()
            {
                Ok = true,
                Message = $"Test success: {tables.Count} tables"
            };
        }
        catch (Exception ex)
        {
            return new UserActionResult()
            {
                Message = ex.Message
            };
        }

    }

    public async Task<Dictionary<string, QTableColumn>> Columns(string slug, string tableName)
    {
        var se = ResolveEngine(slug);
        var columns = await se.Columns(tableName);
        return columns;
    }

    public async Task<List<QTableSchema>> Tables(string slug)
    {
        var se = ResolveEngine(slug);
        var tables = await se.Tables();
        return tables;
    }

    public async Task<string?> ViewDefinition(string slug, string? schemaName, string tableName)
    {
        var se = ResolveEngine(slug);
        return await se.ViewDefinition(schemaName ?? "", tableName);
    }

    public async Task<QDatabaseStructure> DatabaseStructure(string slug)
    {
        if (_structureCache.TryGetValue(slug, out var cached) && DateTime.UtcNow - cached.At < StructureCacheTtl)
        {
            return cached.Structure;
        }

        return await RefreshStructure(slug);
    }

    /// <summary>Перечитать структуру у базы, минуя кэш (кнопка «обновить» в UI).</summary>
    public async Task<QDatabaseStructure> RefreshStructure(string slug)
    {
        var structure = await ResolveEngine(slug).DatabaseStructure();

        _structureCache[slug] = (structure, DateTime.UtcNow);

        return structure;
    }

    public async Task<QueryResultDto> Query(string slug, SqlRequest request, CancellationToken cancellationToken = default)
    {
        var se = ResolveEngine(slug);
        var result = await se.Query(request, cancellationToken);
        return result;
    }

    public async Task<SqlNonQueryResultActionDto> NonQuery(string slug, string sql, IReadOnlyList<SqlParam>? parameters = null, CancellationToken cancellationToken = default)
    {
        var se = ResolveEngine(slug);
        var result = await se.NonQuery(sql, parameters, cancellationToken);

        // DDL мог поменять состав объектов: без сброса дерево до TTL показывало бы старое.
        if (result.Ok && DdlKeywords.Contains(SqlSafety.FirstWord(sql)))
        {
            _structureCache.TryRemove(slug, out _);
        }

        return result;
    }

    public async Task<UserActionResult<string[][]>> ExecuteAction(string slug, DatasourceActionRequest action, CancellationToken cancellationToken)
    {
        try
        {
            UsefulQueries q = new();

            string? foundQuery = action.ActionId switch
            {
                "pg_size_pretty" => q.pg_size_pretty(),
                "pg_database_size" => q.pg_database_size(),
                "pg_namespaces_sizes" => q.pg_namespaces_sizes(),
                "pg_total_relation_size" => q.pg_total_relation_size(),
                "connections_count" => q.connections_count(),
                "query_in_running" => q.query_in_running(),
                "check_db_timezone" => q.check_db_timezone(),
                _ => null
            };

            if (foundQuery is not null)
            {
                var config = GetConfig(slug);

                if (config.Driver != "psql")
                {
                    return Fail($"Действие \"{action.ActionId}\" доступно только для PostgreSQL, а источник \"{config.Slug}\" — {config.Driver}");
                }

                var result = await Query(slug, new SqlRequest { Sql = foundQuery }, cancellationToken);
                return new UserActionResult<string[][]>
                {
                    Ok = result.Ok,
                    Message = result.Message,
                    Data = result.Data ?? []
                };
            }

            if (action.ActionId == "test")
            {
                return new UserActionResult<string[][]>
                {
                    Message = "test successfully",
                    Ok = true,
                    Data = [["data ok"]]
                };
            }
            else if (action.ActionId == "BackupAsSQLFile")
            {
                var config = GetConfig(slug);

                if (config.Driver != "psql")
                {
                    return Fail($"Резервная копия доступна только для PostgreSQL, а источник \"{config.Slug}\" — {config.Driver}");
                }

                string dateTimeFormat = "yyyy-MM-ddTHH-mm-ss";
                string templateFilename = string.Format("{0}_{1}.sql", config.GetDatabaseName(), DateTime.Now.ToString(dateTimeFormat));
                string filePath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", templateFilename);

                await _databaseBackupService.Backup(config, new Mars.Datasource.Abstractions.Models.BackupSettings
                {
                    DumpMode = DumpMode.SchemaAndData,
                    Mode = BackupOutputMode.PlainSql,
                    FilePath = filePath
                }, cancellationToken);

                return UserActionResult<string[][]>.Success([[$"file = {filePath}"]]);
            }
            else
            {
                return Fail($"Action \"{action.ActionId}\" not found");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            //throw;
            return new UserActionResult<string[][]>
            {
                Message = ex.Message,
                Data = [],
            };
        }
    }

    static UserActionResult<string[][]> Fail(string message) => new()
    {
        Ok = false,
        Message = message,
        Data = [],
    };

    public IEnumerable<SelectDatasourceDto> ListSelectDatasource()
    {
        yield return new SelectDatasourceDto(_defaultConfig);

        foreach (var d in configs.Values)
        {
            if (d.Disabled) continue;
            yield return new SelectDatasourceDto(d);
        }
    }
}

//internal static class QTableColumnExtensions
//{
//    public static QTableColumn QTableColumn(NpgsqlDbColumn column)
//    {
//        QTableColumn _this = new();
//        _this.ColumnName = column.ColumnName;
//        _this.ColumnOrdinal = column.ColumnOrdinal ?? 0;
//        _this.ColumnSize = column.ColumnSize;
//        _this.IsAutoIncrement = column.IsAutoIncrement;
//        _this.IsKey = column.IsKey;
//        _this.IsLong = column.IsLong;
//        _this.IsUnique = column.IsUnique;
//        _this.DataType = column.DataType!;
//        _this.DataTypeName = column.DataTypeName;
//        return _this;

//    }
//}

//internal static class QTableSchemaExtensions
//{
//    public static QTableSchema QTableSchema(NpgsqlDataReader reader)
//    {
//        QTableSchema _this = new();
//        _this.SchemaName = reader.GetString(0);
//        _this.TableName = reader.GetString(1);
//        _this.TableOwner = reader.GetString(2);
//        //_this.TableSpace = reader.GetString(3);
//        _this.HasIndexes = reader.GetBoolean(4);
//        _this.HasRules = reader.GetBoolean(5);
//        _this.HasTriggers = reader.GetBoolean(6);
//        _this.RowSecurity = reader.GetBoolean(7);

//        return _this;
//    }
//}

/*
 SELECT row_to_json(X)
FROM (
	SELECT "Email","FirstName",
		(select array_agg(row_to_json(z)) as "files.dima" from (select "FileName", "UserId" from "Files" ) as z)
	FROM "AspNetUsers" WHERE "Id" = 'bee3581d-165a-4f41-9476-967d5177c6b6'
) as X
 */

/*

SELECT row_to_json(X) as data
FROM (
	SELECT *
        ,(select array_agg(row_to_json(z)) as "files_dima"
        from (select "FirstName", "Id" from "AspNetUsers" )
        as z)
	FROM "AnketaQuestions"
) as X
*/

/*
 PostgreSQL 13 supports natively gen_random_uuid ():
-- SELECT gen_random_uuid () as IDD FROM "AspNetUsers"

 */
