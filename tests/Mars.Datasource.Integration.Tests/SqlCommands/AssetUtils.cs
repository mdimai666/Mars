namespace Mars.Datasource.Integration.Tests.SqlCommands;

public static class AssetUtils
{
    public static string Dir()
    {
        var marsDir = Path.GetFullPath("../../..", Environment.CurrentDirectory);

        var sqlCommandsDir = @"SqlCommands";

        return Path.Combine(marsDir, sqlCommandsDir);
    }

    public static string GetSqlCommandScript(string pathFromSqlCommands)
    {
        var dir = Dir();
        var f = Path.Combine(dir, NormalizeAnyPlatformPath(pathFromSqlCommands.TrimStart('/').TrimStart('\\')));
        return File.ReadAllText(f);
    }

    static string NormalizeAnyPlatformPath(string path)
    {
        //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        if (OperatingSystem.IsWindows())
        {
            return path.Replace('/', '\\');
        }
        else
        {
            return path.Replace('\\', '/');
        }
    }
}
