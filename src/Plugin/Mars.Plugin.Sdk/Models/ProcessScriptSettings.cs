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
    }
}
