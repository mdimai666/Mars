namespace Mars.Server.Models;

public class AppDatabaseMigrationOptions
{
    public const string SectionName = "AppDatabaseMigrationOptions";

    public bool AutoMigrate { get; set; }
}
