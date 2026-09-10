namespace Mars.Plugin.Sdk.Models;

public class ProcessScriptSettings
{
    public readonly string ProjectName;
    public readonly string OutDir;
    public readonly string ProjectDir;

    // pack zip / pack nuget
    public readonly string? PackageId;
    public readonly string? PackageVersion;
    public readonly string? Authors;
    public readonly string? Title;
    public readonly string? Description;
    public readonly string? Tags;
    public readonly string? Icon;

    // pack nuget: дополнительные метаданные nuspec (из стандартных свойств пакетов)
    public readonly string? LicenseExpression;
    public readonly string? LicenseFile;
    public readonly string? ProjectUrl;
    public readonly string? RepositoryUrl;
    public readonly string? RepositoryType;
    public readonly string? RepositoryBranch;
    public readonly string? RepositoryCommit;
    public readonly string? ReadmeFile;
    public readonly string? Copyright;
    public readonly string? ReleaseNotes;

    public ProcessScriptSettings(string[] args)
    {
        var argsDict = string.Join(' ', args).Split("--", StringSplitOptions.TrimEntries).Select(arg =>
        {
            var x = arg.Split('=', 2);
            return new KeyValuePair<string, string?>(x[0], x.Length == 2 ? x[1].Trim('"') : null);
        }).ToDictionary();

        ProjectName = argsDict["ProjectName"]!;
        OutDir = argsDict["out"]!;
        ProjectDir = argsDict["ProjectDir"]!;

        PackageId = argsDict.GetValueOrDefault("PackageId");
        PackageVersion = argsDict.GetValueOrDefault("Version");
        Authors = argsDict.GetValueOrDefault("Authors");
        Title = argsDict.GetValueOrDefault("Title");
        Description = argsDict.GetValueOrDefault("Description");
        Tags = argsDict.GetValueOrDefault("Tags");
        Icon = argsDict.GetValueOrDefault("Icon");

        LicenseExpression = argsDict.GetValueOrDefault("LicenseExpression");
        LicenseFile = argsDict.GetValueOrDefault("LicenseFile");
        ProjectUrl = argsDict.GetValueOrDefault("ProjectUrl");
        RepositoryUrl = argsDict.GetValueOrDefault("RepositoryUrl");
        RepositoryType = argsDict.GetValueOrDefault("RepositoryType");
        RepositoryBranch = argsDict.GetValueOrDefault("RepositoryBranch");
        RepositoryCommit = argsDict.GetValueOrDefault("RepositoryCommit");
        ReadmeFile = argsDict.GetValueOrDefault("ReadmeFile");
        Copyright = argsDict.GetValueOrDefault("Copyright");
        ReleaseNotes = argsDict.GetValueOrDefault("ReleaseNotes");
    }
}
