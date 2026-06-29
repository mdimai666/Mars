using System.Text.RegularExpressions;

namespace Mars.Nodes.Core.Implements.Utils;

public class FileListUtility
{
    private readonly List<GitIgnorePattern> _ignorePatterns = [];
    private string _repositoryRoot = default!;
    public bool EnableDebugOutput { get; set; } = false;

    /// <summary>
    /// Retrive file list by path
    /// </summary>
    /// <param name="path">dir</param>
    /// <param name="includeFilter">
    /// <list type="bullet">
    /// <item>* - all</item>
    /// <item>.mp3,.wav - some extensions</item>
    /// </list>
    /// </param>
    /// <param name="maxDepth">0=all, 1=one level, 3=three levels</param>
    /// <param name="returnRelativePaths"></param>
    /// <param name="useRootGitIgnore"></param>
    /// <returns></returns>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public string[] GetFiles(string path,
                                string includeFilter = "*",
                                int maxDepth = 0,
                                bool returnRelativePaths = false,
                                bool useRootGitIgnore = false)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Directory not found: {path}");

        // Нормализуем путь (убираем trailing slash для корректного сравнения)
        path = Path.GetFullPath(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var pathWithSeparator = path + Path.DirectorySeparatorChar;

        // Находим корень репозитория
        if (useRootGitIgnore)
            _repositoryRoot = FindRepositoryRoot(path) ?? path;

        if (EnableDebugOutput)
            Console.WriteLine($"Repository root: {_repositoryRoot}");

        // Загружаем .gitignore паттерны
        LoadGitIgnorePatterns(path);

        // Парсим фильтр расширений
        var extensions = ParseIncludeFilter(includeFilter);

        // Получаем все файлы
        var allFiles = Directory.GetFiles(path, "*.*", maxDepth == 1 ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);

        // Фильтруем по глубине, если нужно
        if (maxDepth > 1)
        {
            allFiles = allFiles.Where(file =>
            {
                // Убираем префикс path + separator
                if (!file.StartsWith(pathWithSeparator, StringComparison.OrdinalIgnoreCase))
                    return false; // на случай странных путей (например, symlink'ов)

                //var rel = Path.GetRelativePath(path, file);
                var rel = file.Substring(pathWithSeparator.Length);
                int depth = rel.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Length;
                return depth <= maxDepth;
            }).ToArray(); // материализуем, чтобы не перечитывать
        }

        if (EnableDebugOutput)
            Console.WriteLine($"Total files found: {allFiles.Length}");

        // Фильтруем по расширениям и .gitignore
        var result = new List<string>();

        foreach (var file in allFiles)
        {
            if (!MatchesExtensionFilter(file, extensions))
                continue;

            bool ignored = IsIgnored(file);

            if (EnableDebugOutput && file.Contains("bin"))
            {
                Console.WriteLine($"\nChecking: {file}");
                Console.WriteLine($"  Ignored: {ignored}");
            }

            if (ignored) continue;

            // Преобразуем в относительный путь, если нужно
            string finalPath = returnRelativePaths
                ? Path.GetRelativePath(path, file)
                : file;

            result.Add(finalPath);
        }

        return result.ToArray();
    }

    private string? FindRepositoryRoot(string startPath)
    {
        var current = new DirectoryInfo(startPath);

        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")) ||
                File.Exists(Path.Combine(current.FullName, ".git")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private void LoadGitIgnorePatterns(string searchPath)
    {
        _ignorePatterns.Clear();

        var gitIgnoreFiles = new List<string>();

        // Собираем все директории от корня репозитория до текущей
        var pathsToCheck = new List<string>();
        var current = new DirectoryInfo(searchPath);

        while (current != null)
        {
            pathsToCheck.Insert(0, current.FullName);

            if (_repositoryRoot != null &&
                current.FullName.Equals(_repositoryRoot, StringComparison.OrdinalIgnoreCase))
                break;

            current = current.Parent;
        }

        // Добавляем .gitignore из каждой директории
        foreach (var dir in pathsToCheck)
        {
            var gitIgnorePath = Path.Combine(dir, ".gitignore");
            if (File.Exists(gitIgnorePath))
                gitIgnoreFiles.Add(gitIgnorePath);
        }

        // Также ищем .gitignore внутри поддиректорий
        var subGitIgnores = Directory.GetFiles(searchPath, ".gitignore", SearchOption.AllDirectories);
        foreach (var gitIgnore in subGitIgnores)
        {
            if (!gitIgnoreFiles.Contains(gitIgnore, StringComparer.OrdinalIgnoreCase))
                gitIgnoreFiles.Add(gitIgnore);
        }

        if (EnableDebugOutput)
        {
            Console.WriteLine($"\n.gitignore files found ({gitIgnoreFiles.Count}):");
            foreach (var gf in gitIgnoreFiles)
                Console.WriteLine($"  {gf}");
        }

        // Парсим все .gitignore файлы
        foreach (var gitIgnoreFile in gitIgnoreFiles)
        {
            var basePath = Path.GetDirectoryName(gitIgnoreFile);

            if (!File.Exists(gitIgnoreFile))
                continue;

            var lines = File.ReadAllLines(gitIgnoreFile);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                    continue;

                var pattern = new GitIgnorePattern(trimmed, basePath, _repositoryRoot);
                _ignorePatterns.Add(pattern);

                if (EnableDebugOutput)
                {
                    var lower = trimmed.ToLower();
                    if (lower.Contains("bin") || lower.Contains("obj") || lower == "[bb]in/" || lower == "[oo]bj/")
                        Console.WriteLine($"  Pattern: '{trimmed}' from {basePath}");
                }
            }
        }

        // Всегда игнорируем .git директорию
        if (_repositoryRoot != null)
            _ignorePatterns.Add(new GitIgnorePattern(".git/", _repositoryRoot, _repositoryRoot));

        if (EnableDebugOutput)
            Console.WriteLine($"\nTotal patterns loaded: {_ignorePatterns.Count}\n");
    }

    private HashSet<string> ParseIncludeFilter(string includeFilter)
    {
        if (string.IsNullOrWhiteSpace(includeFilter) || includeFilter == "*")
            return ["*"];

        return includeFilter
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(ext => ext.Trim().ToLowerInvariant())
            .Select(ext => ext.StartsWith(".") ? ext : "." + ext)
            .ToHashSet();
    }

    private bool MatchesExtensionFilter(string filePath, HashSet<string> extensions)
    {
        if (extensions.Contains("*"))
            return true;

        var fileExt = Path.GetExtension(filePath).ToLowerInvariant();
        return extensions.Contains(fileExt);
    }

    private bool IsIgnored(string filePath)
    {
        var basePath = _repositoryRoot ?? Path.GetDirectoryName(filePath)!;
        bool isIgnored = false;
        var matchedPatterns = new List<string>();

        foreach (var pattern in _ignorePatterns)
        {
            bool matches = pattern.Matches(filePath, basePath);

            if (matches)
            {
                matchedPatterns.Add($"{pattern.OriginalPattern} ({(pattern.IsNegation ? "negation" : "ignore")})");

                if (pattern.IsNegation)
                    isIgnored = false;
                else
                    isIgnored = true;
            }
        }

        if (EnableDebugOutput && filePath.Contains("bin") && matchedPatterns.Count > 0)
        {
            Console.WriteLine($"    Matched patterns:");
            foreach (var p in matchedPatterns)
                Console.WriteLine($"      - {p}");
        }

        return isIgnored;
    }

    private class GitIgnorePattern
    {
        public string Pattern { get; }
        public string OriginalPattern { get; }
        public string BasePath { get; }
        public bool IsNegation { get; }
        public bool IsDirectoryOnly { get; }
        private readonly Regex _regex;
        private readonly string _repositoryRoot;

        public GitIgnorePattern(string pattern, string basePath, string repositoryRoot)
        {
            BasePath = basePath;
            OriginalPattern = pattern;
            _repositoryRoot = repositoryRoot;

            // Обработка negation паттернов
            if (pattern.StartsWith("!"))
            {
                IsNegation = true;
                pattern = pattern.Substring(1);
            }

            // Проверка, только для директорий
            if (pattern.EndsWith("/"))
            {
                IsDirectoryOnly = true;
                pattern = pattern.TrimEnd('/');
            }

            Pattern = pattern;
            _regex = ConvertToRegex(pattern);
        }

        public bool Matches(string filePath, string repositoryRoot)
        {
            // Получаем путь относительно директории с .gitignore
            var relativeToBase = GetRelativePath(BasePath, filePath);

            // Получаем путь относительно корня репозитория
            var relativeToRoot = GetRelativePath(_repositoryRoot ?? repositoryRoot, filePath);

            // Проверяем совпадение
            bool matchBase = _regex.IsMatch(relativeToBase);
            bool matchRoot = _regex.IsMatch(relativeToRoot);
            bool match = matchBase || matchRoot;

            // Для директорий проверяем, содержится ли паттерн в пути
            if (IsDirectoryOnly)
            {
                var parts = relativeToRoot.Split('/');
                bool matchPart = parts.Any(p => _regex.IsMatch(p));
                match = match || matchPart;
            }

            return match;
        }

        private string GetRelativePath(string basePath, string fullPath)
        {
            if (string.IsNullOrEmpty(basePath))
                return fullPath;

            var relative = Path.GetRelativePath(basePath, fullPath);
            return relative.Replace('\\', '/');
        }

        private Regex ConvertToRegex(string gitPattern)
        {
            var regexPattern = "";

            // Если паттерн начинается с /, он относителен к директории .gitignore
            bool isRooted = gitPattern.StartsWith("/");
            if (isRooted)
            {
                gitPattern = gitPattern.Substring(1);
                regexPattern = "^";
            }
            else
            {
                // Паттерн может совпадать на любом уровне
                regexPattern = "(^|/)";
            }

            // Конвертируем glob паттерн в regex
            int i = 0;
            while (i < gitPattern.Length)
            {
                char c = gitPattern[i];

                if (c == '*')
                {
                    if (i + 1 < gitPattern.Length && gitPattern[i + 1] == '*')
                    {
                        // ** совпадает с любыми директориями
                        i += 2;

                        if (i < gitPattern.Length && gitPattern[i] == '/')
                        {
                            regexPattern += "(.*/)?";
                            i++;
                        }
                        else
                        {
                            regexPattern += ".*";
                        }
                    }
                    else
                    {
                        // * совпадает с чем угодно кроме /
                        regexPattern += "[^/]*";
                        i++;
                    }
                }
                else if (c == '?')
                {
                    regexPattern += "[^/]";
                    i++;
                }
                else if (c == '[')
                {
                    // Character class [abc] или [a-z]
                    int closeBracket = gitPattern.IndexOf(']', i);
                    if (closeBracket > i)
                    {
                        var charClass = gitPattern.Substring(i, closeBracket - i + 1);
                        regexPattern += charClass;
                        i = closeBracket + 1;
                    }
                    else
                    {
                        regexPattern += Regex.Escape(c.ToString());
                        i++;
                    }
                }
                else if (c == '.')
                {
                    regexPattern += "\\.";
                    i++;
                }
                else if (c == '/')
                {
                    regexPattern += "/";
                    i++;
                }
                else
                {
                    regexPattern += Regex.Escape(c.ToString());
                    i++;
                }
            }

            // Паттерн может совпадать с файлом или директорией
            regexPattern += "(/.*)?$";

            return new Regex(regexPattern, RegexOptions.IgnoreCase);
        }
    }
}
