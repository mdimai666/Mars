using Mars.Datasource.Abstractions.Exceptions;
using Mars.Datasource.Abstractions.Models;

namespace Mars.Datasource.Abstractions.Interfaces;

public interface IDatasourceBackupDriver
{
    /// <exception cref="DatasourceOperationException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    public Task Backup(string connectionString, BackupSettings settings, CancellationToken cancellationToken = default);

    /// <exception cref="DatasourceOperationException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    public Task Restore(string connectionString, RestoreSettings settings, CancellationToken cancellationToken = default);

}