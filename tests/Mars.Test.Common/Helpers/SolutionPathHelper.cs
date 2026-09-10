namespace Mars.Test.Common.Helpers;

/// <summary>
/// Резолвит пути относительно корня solution (каталог с Mars.slnx).
/// Замена хрупких цепочек "..", которые зависят от CWD и глубины bin/{Configuration}/{TFM}.
/// </summary>
public static class SolutionPathHelper
{
    private const string SolutionFileName = "Mars.slnx";
    private static readonly Lazy<string> _solutionRoot = new(FindSolutionRoot);

    /// <summary>Корень solution — каталог, содержащий <c>Mars.slnx</c>.</summary>
    public static string SolutionRoot => _solutionRoot.Value;

    /// <summary>
    /// Абсолютный путь из сегментов относительно корня solution:
    /// <c>Resolve("tests", "Mars.Integration.Tests", "Controllers", "Medias", "ExampleFiles")</c>.
    /// </summary>
    public static string Resolve(params string[] segments)
    {
        return Path.GetFullPath(Path.Combine([SolutionRoot, .. segments]));
    }

    private static string FindSolutionRoot()
    {
        // От CWD не зависим: приложение меняет его при старте (FixDebugModeBaseDirectory) —
        // поднимаемся от каталога тестовой сборки, пока не найдём файл solution.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, SolutionFileName)))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            $"Не найден корень solution: файл '{SolutionFileName}' отсутствует выше '{AppContext.BaseDirectory}'.");
    }
}
