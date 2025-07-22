using Mars.Datasource.Core;
using Mars.Datasource.Core.Dto;
using Mars.Datasource.Core.Interfaces;
using Mars.Datasource.Host.Core.Models;
using Mars.Datasource.Host.MsSQL;
using Mars.Datasource.Host.MySQL;
using Mars.Datasource.Host.PostgreSQL;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Microsoft.Extensions.Configuration;

namespace Mars.Datasource.Host.Services;

/// <summary>
/// singletone
/// </summary>
internal class DatasourceService : IDatasourceService
{
    private readonly IOptionService _optionService;
    private readonly IDatabaseBackupService _databaseBackupService;

    string _connectionString;
    DatasourceConfig _defaultConfig;
    DatasourceOption? _optionValue;
    public DatasourceConfig DefaultConfig => _defaultConfig;

    Dictionary<string, DatasourceConfig>? _configsCache;
    Dictionary<string, DatasourceConfig> configs
    {
        get
        {
            AutoUpdateConfig();
            return _configsCache!;
        }
    }

    public DatasourceService(IConfiguration configuration, IOptionService optionService, IDatabaseBackupService databaseBackupService)
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
    }

    public void InvalidateLocalDictCache(DatasourceOption opt)
    {
        _optionValue = opt;
        _configsCache = opt.Configs.ToDictionary(s => s.Slug, s => s);
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

    IDatasourceDriver ResolveEngine(string slug)
    {
        if (slug == "default")
        {
            return ResolveEngine(_defaultConfig);
        }

        if (configs.TryGetValue(slug, out var config))
        {
            return ResolveEngine(config);
        }
        throw new ArgumentNullException("slug not found in DataSource configs");
    }

    IDatasourceDriver ResolveEngine(DatasourceConfig config)
    {
        return config.Driver switch
        {
            "psql" => new DatasourcePostgreSQLDriver(config),
            "mssql" => new DatasourceMsSQLDriver(config),
            "mysql" => new DatasourceMySQLDriver(config),
            _ => throw new NotImplementedException($"Driver \"{config.Driver}\" not found")
        };
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

    public async Task<QDatabaseStructure> DatabaseStructure(string slug)
    {
        var se = ResolveEngine(slug);
        var structure = await se.DatabaseStructure();
        return structure;
    }

    public async Task<SqlQueryResultActionDto> SqlQuery(string slug, string sql)
    {
        var se = ResolveEngine(slug);
        var result = await se.SqlQuery(sql);
        return result;
    }

    public async Task<UserActionResult<string[][]>> ExecuteAction(ExecuteActionRequest action)
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
                var result = await SqlQuery("default", foundQuery);
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
                string dateTimeFormat = "yyyy-MM-ddTHH-mm-ss";
                string templateFilename = string.Format("{0}_{1}.sql", DefaultConfig.GetDatabaseName(), DateTime.Now.ToString(dateTimeFormat));
                string filePath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", templateFilename);

                await _databaseBackupService.Backup(_defaultConfig, new Core.Models.BackupSettings
                {
                    DumpMode = DumpMode.SchemaAndData,
                    Mode = BackupOutputMode.PlainSql,
                    FilePath = filePath
                });

                return UserActionResult<string[][]>.Success([[$"file = {filePath}"]]);
            }
            else
            {
                return new UserActionResult<string[][]>
                {
                    Message = $"Action \"{action.ActionId}\" not found",
                    Ok = false
                };
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            //throw;
            return new UserActionResult<string[][]>
            {
                Message = ex.Message
            };
        }
    }

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
