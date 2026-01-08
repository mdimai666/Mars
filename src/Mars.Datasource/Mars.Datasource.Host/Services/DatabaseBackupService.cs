using Mars.Datasource.Core;
using Mars.Datasource.Core.Interfaces;
using Mars.Datasource.Host.Core.Models;
using Mars.Datasource.Host.PostgreSQL;

namespace Mars.Datasource.Host.Services;

internal class DatabaseBackupService : IDatabaseBackupService
{
    IDatasourceBackupDriver ResolveEngine(DatasourceConfig config)
    {
        return config.Driver switch
        {
            "psql" => new DatasourcePostgreSQLBackupDriver(),
            //"mssql" => new DatasourceMsSQLDriver(config),
            //"mysql" => new DatasourceMySQLDriver(config),
            _ => throw new NotImplementedException($"DatabaseBackupService for Driver \"{config.Driver}\" not found")
        };
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
