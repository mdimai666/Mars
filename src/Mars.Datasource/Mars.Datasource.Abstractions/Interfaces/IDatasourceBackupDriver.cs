using Mars.Datasource.Abstractions.Exceptions;
using Mars.Datasource.Abstractions.Mappings;
using Mars.Datasource.Abstractions.Sql;
using Mars.Datasource.Contracts.Sql;

namespace Mars.Datasource.Abstractions.Interfaces;

public interface IDatasourceBackupDriver
{
    /// <summary>Ключ движка, для которого умеет backup/restore (`psql`).</summary>
    public string Driver { get; }

    /// <exception cref="DatasourceOperationException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    public Task Backup(string connectionString, BackupSettings settings, CancellationToken cancellationToken = default);

    /// <exception cref="DatasourceOperationException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    public Task Restore(string connectionString, RestoreSettings settings, CancellationToken cancellationToken = default);

}