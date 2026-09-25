using Mars.Datasource.Abstractions.Mappings;
using Mars.Datasource.Abstractions.Sql;
using Mars.Datasource.Contracts.Sql;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;

namespace Mars.Datasource.Host.Services;

public interface IDatabaseBackupService
{
    public Task Backup(DatasourceConfig datasourceConfig, BackupSettings settings, CancellationToken cancellationToken);
    public Task Restore(DatasourceConfig datasourceConfig, RestoreSettings settings, CancellationToken cancellationToken);
}
