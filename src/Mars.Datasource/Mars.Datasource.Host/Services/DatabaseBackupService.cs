using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Abstractions.Models;
using Mars.Datasource.Contracts.Models;

namespace Mars.Datasource.Host.Services;

internal class DatabaseBackupService : IDatabaseBackupService
{
    readonly Dictionary<string, IDatasourceBackupDriver> _drivers;

    public DatabaseBackupService(IEnumerable<IDatasourceBackupDriver> drivers)
    {
        _drivers = drivers.ToDictionary(d => d.Driver, StringComparer.OrdinalIgnoreCase);
    }

    IDatasourceBackupDriver ResolveEngine(DatasourceConfig config)
    {
        if (_drivers.TryGetValue(config.Driver, out var driver)) return driver;

        throw new NotSupportedException($"Backup/restore для источника \"{config.Driver}\" не поддерживается");
    }

    public Task Backup(DatasourceConfig datasourceConfig, BackupSettings settings, CancellationToken cancellationToken)
    {
        var driver = ResolveEngine(datasourceConfig);
        return driver.Backup(datasourceConfig.ConnectionString, settings, cancellationToken);
    }

    public Task Restore(DatasourceConfig datasourceConfig, RestoreSettings settings, CancellationToken cancellationToken)
    {
        var driver = ResolveEngine(datasourceConfig);
        return driver.Restore(datasourceConfig.ConnectionString, settings, cancellationToken);
    }
}
