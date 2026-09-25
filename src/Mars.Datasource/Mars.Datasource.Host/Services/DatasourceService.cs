using System.Collections.Concurrent;
using Mars.Contracts.Common;
using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Abstractions.Mappings;
using Mars.Datasource.Abstractions.Sql;
using Mars.Datasource.Contracts.Sql;
using Mars.Datasource.Abstractions.Services;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Options.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Mars.Datasource.Host.Services;

/// <summary>
/// Сервис источников: реестр конфигов, каталог и выполнение запросов. Один singleton на три
/// контракта — потребителю видна только нужная грань (<see cref="IDatasourceRegistry"/>,
/// <see cref="IDatasourceService"/>, <see cref="ISqlDatasourceService"/>).
/// </summary>
internal class DatasourceService : IDatasourceRegistry, IDatasourceService, ISqlDatasourceService
{
    /// <summary>Резервная копия базы: файл пишет хост, поэтому действие живёт здесь, а не у провайдера.</summary>
    public const string BackupActionId = "BackupAsSQLFile";

    static readonly DatasourceActionDescriptor BackupAction = new()
    {
        Id = BackupActionId,
        Label = BackupActionId,
        Description = "Резервная копия базы в файл .sql (папка Downloads)",
    };

    private readonly IOptionService _optionService;
    private readonly IDatabaseBackupService _databaseBackupService;

    /// <summary>Провайдеры по типу источника и драйверу: ядро модуля их не создаёт, их регистрирует корень композиции.</summary>
    readonly IDatasourceProviderRegistry _registry;

    /// <summary>Служебные тела источника (документ запросов) в data-корне.</summary>
    readonly IDatasourceStore _store;

    readonly ILogger<DatasourceService> _logger;

    string _connectionString;
    DatasourceConfig _defaultConfig;
    DatasourceOption? _optionValue;
    public DatasourceConfig DefaultConfig => _defaultConfig;

    Dictionary<string, DatasourceConfig>? _configsCache;

    /// <summary>Каталог — дерево объектов источника: у не-sql источников он дороже структуры базы.</summary>
    readonly ConcurrentDictionary<string, (DatasourceCatalog Catalog, DateTime At)> _catalogCache = new(StringComparer.OrdinalIgnoreCase);

    static readonly TimeSpan CatalogCacheTtl = TimeSpan.FromSeconds(30);

    /// <summary>Первый оператор, после которого кэш каталога уже неактуален.</summary>
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
        IDatasourceProviderRegistry registry, IDatasourceStore store, ILogger<DatasourceService> logger)
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
        _registry = registry;
        _store = store;
        _logger = logger;
    }

    //=== реестр источников ====================================================

    public IReadOnlyCollection<DatasourceKindProfile> Providers() => _registry.Describe();

    public void InvalidateLocalDictCache(DatasourceOption opt)
    {
        _optionValue = opt;

        // Битые и повторяющиеся slug пропускаем: раньше ToDictionary падал и «ломались» все источники сразу.
        Dictionary<string, DatasourceConfig> configs = new(StringComparer.OrdinalIgnoreCase);

        foreach (var config in opt.Configs)
        {
            config.Normalize();

            if (DatasourceConfig.ValidateSlug(config.Slug) is not null) continue;

            configs.TryAdd(config.Slug, config);
        }

        _configsCache = configs;

        // Сменились настройки источников — каталог мог измениться.
        _catalogCache.Clear();
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

    public async Task<UserActionResult> TestConnection(ConnectionStringTestDto dto)
    {
        var tmpConfig = new DatasourceConfig()
        {
            Kind = dto.Kind,
            ConnectionString = dto.ConnectionString,
            Driver = dto.Driver,
            Settings = dto.Settings,
            Title = "TestConnection " + dto.Driver,
            Slug = "test_" + (string.IsNullOrWhiteSpace(dto.Driver) ? dto.Kind : dto.Driver),
        };

        tmpConfig.Normalize();

        try
        {
            var provider = _registry.Resolve(tmpConfig);

            // Проверка подключения — всегда живой запрос: сохранённый каталог её не заменяет.
            var catalog = provider is IDatasourceDiscoverableProvider discoverable
                ? await discoverable.Discover()
                : await provider.Catalog();

            var objects = catalog.Groups.Sum(group => group.Objects.Count);

            return new UserActionResult()
            {
                Ok = true,
                Message = $"Test success: {objects} objects"
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

    public IEnumerable<SelectDatasourceDto> ListSelectDatasource()
    {
        yield return new SelectDatasourceDto(_defaultConfig);

        foreach (var d in configs.Values)
        {
            if (d.Disabled) continue;
            yield return new SelectDatasourceDto(d);
        }
    }

    //=== каталог и запросы ====================================================

    public async Task<DatasourceCatalog> Catalog(string slug)
    {
        if (_catalogCache.TryGetValue(slug, out var cached) && DateTime.UtcNow - cached.At < CatalogCacheTtl)
        {
            return cached.Catalog;
        }

        return await RefreshCatalog(slug);
    }

    public async Task<DatasourceCatalog> RefreshCatalog(string slug)
    {
        var catalog = await LoadCatalogAsync(GetConfig(slug));

        _catalogCache[slug] = (catalog, DateTime.UtcNow);

        return catalog;
    }

    /// <summary>
    /// Каталог источника: у провайдера с discovery «обновить» перечитывает описание API,
    /// а не сохранённый каталог. Действия хоста (резервная копия) добавляются к действиям провайдера.
    /// </summary>
    async Task<DatasourceCatalog> LoadCatalogAsync(DatasourceConfig config)
    {
        var provider = _registry.Resolve(config);

        var catalog = provider is IDatasourceDiscoverableProvider discoverable
            ? await discoverable.Discover()
            : await provider.Catalog();

        if (CanBackup(config)) catalog.Actions.Add(BackupAction);

        return catalog;
    }

    public async Task<QueryResultDto> Query(string slug, DatasourceRequest request, CancellationToken cancellationToken = default)
    {
        var provider = _registry.Resolve(GetConfig(slug));

        return await provider.Query(request, cancellationToken);
    }

    public async Task<DatasourceModifyResult> Modify(string slug, DatasourceRequest request, CancellationToken cancellationToken = default)
    {
        var provider = _registry.Resolve(GetConfig(slug));
        var result = await provider.Modify(request, cancellationToken);

        // DDL мог поменять состав объектов: без сброса дерево до TTL показывало бы старое.
        if (result.Ok && IsSql(request) && DdlKeywords.Contains(SqlSafety.FirstWord(request.Query)))
        {
            _catalogCache.TryRemove(slug, out _);
        }

        return result;
    }

    public async Task<UserActionResult<string[][]>> ExecuteAction(string slug, DatasourceActionRequest action, CancellationToken cancellationToken = default)
    {
        var config = GetConfig(slug);

        try
        {
            if (string.Equals(action.ActionId, BackupActionId, StringComparison.OrdinalIgnoreCase))
            {
                return await BackupAsFileAsync(config, cancellationToken);
            }

            var provider = _registry.Resolve(config);

            if (provider is not IDatasourceActionProvider actions)
            {
                return Fail($"У источника \"{config.Slug}\" нет действий");
            }

            return await actions.ExecuteAction(action, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Datasource action {ActionId} failed for {Slug}", action.ActionId, slug);

            return Fail(ex.Message);
        }
    }

    //=== документ запросов пользователя =======================================

    public async Task<string> Document(string slug, string name)
    {
        var config = GetConfig(slug);

        return await _store.ReadTextAsync(config.Slug, DocumentName(config, name)) ?? "";
    }

    public async Task<UserActionResult> SaveDocument(string slug, string name, string content)
    {
        var config = GetConfig(slug);

        await _store.WriteTextAsync(config.Slug, DocumentName(config, name), content ?? "");

        // Дерево строится в том числе из документа — следующий запрос каталога обязан показать правки.
        _catalogCache.TryRemove(slug, out _);

        return UserActionResult.Success($"Документ сохранён: {config.Title}");
    }

    /// <summary>
    /// Имя документа берём из профиля провайдера: произвольное имя из запроса не должно писать
    /// в data-корень, а источник без документа — принимать его вовсе.
    /// </summary>
    string DocumentName(DatasourceConfig config, string name)
    {
        var declared = _registry.Resolve(config).Profile.DocumentName;

        if (string.IsNullOrEmpty(declared) || !string.Equals(declared, name, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"Источник \"{config.Slug}\" не хранит документ \"{name}\"");
        }

        return declared;
    }

    //=== sql-специфика ========================================================

    public async Task<string?> ViewDefinition(string slug, string? schemaName, string tableName)
    {
        var driver = _registry.ResolveSql(GetConfig(slug)).Driver;

        return await driver.ViewDefinition(schemaName ?? "", tableName);
    }

    //=== backup ===============================================================

    static bool CanBackup(DatasourceConfig config)
        => string.Equals(config.Kind, DatasourceKind.Sql, StringComparison.OrdinalIgnoreCase)
           && string.Equals(config.Driver, "psql", StringComparison.OrdinalIgnoreCase);

    async Task<UserActionResult<string[][]>> BackupAsFileAsync(DatasourceConfig config, CancellationToken cancellationToken)
    {
        if (!CanBackup(config))
        {
            return Fail($"Резервная копия доступна только для PostgreSQL, а источник \"{config.Slug}\" — {ProviderName(config)}");
        }

        var fileName = $"{config.GetDatabaseName()}_{DateTime.Now:yyyy-MM-ddTHH-mm-ss}.sql";
        var filePath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", fileName);

        await _databaseBackupService.Backup(config, new BackupSettings
        {
            DumpMode = DumpMode.SchemaAndData,
            Mode = BackupOutputMode.PlainSql,
            FilePath = filePath
        }, cancellationToken);

        return UserActionResult<string[][]>.Success([[$"file = {filePath}"]]);
    }

    static UserActionResult<string[][]> Fail(string message) => new()
    {
        Ok = false,
        Message = message,
        Data = [],
    };

    static bool IsSql(DatasourceRequest request)
        => string.IsNullOrWhiteSpace(request.Language)
           || string.Equals(request.Language, DatasourceLanguage.Sql, StringComparison.OrdinalIgnoreCase);

    static string ProviderName(DatasourceConfig config)
        => string.Equals(config.Kind, DatasourceKind.Sql, StringComparison.OrdinalIgnoreCase) ? config.Driver : config.Kind;
}
