using Mars.Test.Common.Helpers;

namespace Mars.Datasource.Integration.Tests.SqlCommands;

public static class AssetUtils
{
    public static string Dir()
    {
        return SolutionPathHelper.Resolve("tests", "Mars.Datasource.Integration.Tests", "SqlCommands");
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
