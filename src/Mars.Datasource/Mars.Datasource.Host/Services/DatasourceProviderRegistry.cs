using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Abstractions.Mappings;
using Mars.Datasource.Abstractions.Sql;
using Mars.Datasource.Contracts.Sql;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;

namespace Mars.Datasource.Host.Services;

/// <summary>
/// Реестр провайдеров: sql-движки приходят фабриками драйверов и оборачиваются здесь,
/// остальные источники регистрируют свои <see cref="IDatasourceProviderFactory"/>.
/// </summary>
internal class DatasourceProviderRegistry : IDatasourceProviderRegistry
{
    readonly List<IDatasourceProviderFactory> _factories;

    public DatasourceProviderRegistry(IEnumerable<IDatasourceDriverFactory> sqlDrivers, IEnumerable<IDatasourceProviderFactory> providers)
    {
        _factories =
        [
            .. sqlDrivers.Select(driver => new SqlDatasourceProviderFactory(driver)),
            .. providers,
        ];
    }

    public IDatasourceProvider Resolve(DatasourceConfig config) => Find(config).Create(config);

    public ISqlDatasourceProvider ResolveSql(DatasourceConfig config)
    {
        if (!string.Equals(Kind(config), DatasourceKind.Sql, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"Операция доступна только для sql-источников, а \"{config.Slug}\" — {Kind(config)}");
        }

        return (ISqlDatasourceProvider)Find(config).Create(config);
    }

    public IReadOnlyCollection<DatasourceKindProfile> Describe()
        => _factories
            .Select(factory => factory.Profile)
            .OrderBy(profile => profile.Kind, StringComparer.OrdinalIgnoreCase)
            .ThenBy(profile => profile.Driver, StringComparer.OrdinalIgnoreCase)
            .ToList();

    static string Kind(DatasourceConfig config)
        => string.IsNullOrWhiteSpace(config.Kind) ? DatasourceKind.Sql : config.Kind.Trim();

    IDatasourceProviderFactory Find(DatasourceConfig config)
    {
        var kind = Kind(config);

        var candidates = _factories
            .Where(factory => string.Equals(factory.Profile.Kind, kind, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (candidates.Count == 0)
        {
            throw new NotSupportedException($"Провайдер источников \"{kind}\" не подключён");
        }

        // Единственный вариант типа источника: драйвер в конфиге не несёт смысла — у настроек,
        // созданных до появления Kind, там по умолчанию стоит "psql".
        if (candidates.Count == 1) return candidates[0];

        if (!string.IsNullOrWhiteSpace(config.Driver))
        {
            return candidates.FirstOrDefault(factory => string.Equals(factory.Profile.Driver, config.Driver, StringComparison.OrdinalIgnoreCase))
                ?? throw new NotSupportedException($"Драйвер \"{config.Driver}\" для источника \"{kind}\" не подключён");
        }

        throw new NotSupportedException($"Источник \"{kind}\" требует указания драйвера: доступно {string.Join(", ", candidates.Select(c => c.Profile.Driver))}");
    }
}
