using Mars.Datasource.Host.Core.Exceptions;
using Mars.Datasource.Host.Core.Models;

namespace Mars.Datasource.Core.Interfaces;

public interface IDatasourceBackupDriver
{
    /// <exception cref="DatasourceOperationException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    public Task Backup(string connectionString, BackupSettings settings, CancellationToken cancellationToken = default);

    /// <exception cref="DatasourceOperationException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    public Task Restore(string connectionString, RestoreSettings settings, CancellationToken cancellationToken = default);

}