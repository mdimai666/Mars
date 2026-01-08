using Mars.Datasource.Core;
using Mars.Datasource.Host.Core.Models;

namespace Mars.Datasource.Host.Services;

public interface IDatabaseBackupService
{
    public Task Backup(DatasourceConfig datasourceConfig, BackupSettings settings, CancellationToken cancellationToken);
    public Task Restore(DatasourceConfig datasourceConfig, RestoreSettings settings, CancellationToken cancellationToken);
}
