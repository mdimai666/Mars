using System.Reflection;

namespace Mars.Plugin.PluginPublishScript.Models;

public class ProcessScriptSettings
{
    public readonly string ProjectName;
    public readonly string OutDir;
    public readonly string ProjectDir;
    //public readonly string PublishDir;

    public readonly string CurrentScriptProjectName;
    public readonly string CurrentScriptProjectNugetName;

    public ProcessScriptSettings(string[] args)
    {
#if DEBUG
        //args = "--run-postpublish --ProjectName=Mars.TelegramPlugin --out=bin\\Release\\net10.0\\ --PublishDir=bin\\Release\\net10.0\\publish\\  --ProjectDir=C:\\Users\\D\\Documents\\VisualStudio\\2025\\Mars.TelegramPlugin\\Mars.TelegramPlugin\\".Split(' ');
#endif
#if DEBUG
        //args = "--run-postdebugcompile --ProjectName=Mars.TelegramPlugin --out=bin\\Debug\\net10.0\\ --PublishDir=  --ProjectDir=C:\\Users\\D\\Documents\\VisualStudio\\2025\\Mars.TelegramPlugin\\Mars.TelegramPlugin\\".Split(' ');
#endif

        var argsDict = string.Join(' ', args).Split("--", StringSplitOptions.TrimEntries).Select(arg =>
        {
            var x = arg.Split('=', 2);
            return new KeyValuePair<string, string?>(x[0], x.Length == 2 ? x[1] : null);
        }).ToDictionary();

        ProjectName = argsDict["ProjectName"]!;
        OutDir = argsDict["out"]!;
        ProjectDir = argsDict["ProjectDir"]!;
        //PublishDir = argsDict["PublishDir"]!;

        CurrentScriptProjectName = Assembly.GetExecutingAssembly().GetName().Name!;
        CurrentScriptProjectNugetName = $"mdimai666.{CurrentScriptProjectName}";
    }
}
