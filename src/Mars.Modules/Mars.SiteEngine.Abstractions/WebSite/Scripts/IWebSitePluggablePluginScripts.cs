namespace Mars.Host.Shared.WebSite.Scripts;

public interface IWebSitePluggablePluginScripts
{
    IReadOnlyDictionary<string, IWebSiteInjectContentPart> Scripts { get; }
    string CompiledHeader { get; }
    string CompiledFooter { get; }
    void AddScript(string name, IWebSiteInjectContentPart script);
    void RemoveScript(string name);
}
