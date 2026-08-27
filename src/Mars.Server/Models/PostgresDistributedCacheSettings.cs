namespace Mars.Host.Models;

public class PostgresDistributedCacheSettings
{
    public const string Section = "PostgresDistributedCache";

    public string SchemaName { get; set; } = "cache";
    public string TableName { get; set; } = "cache";
    public bool CreateIfNotExists { get; set; } = true;
    public bool UseWAL { get; set; } = false;
    public string ExpiredItemsDeletionInterval { get; set; } = "00:30:00";
    public string DefaultSlidingExpiration { get; set; } = "00:20:00";
}
