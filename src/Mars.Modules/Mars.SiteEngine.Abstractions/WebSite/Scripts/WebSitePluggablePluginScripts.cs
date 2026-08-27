using Mars.Core.Extensions;

namespace Mars.Host.Shared.WebSite.Scripts;

public abstract class WebSitePluggablePluginScripts(ISiteScriptsBuilder siteScriptsBuilder) : IWebSitePluggablePluginScripts
{
    Dictionary<string, IWebSiteInjectContentPart> _scripts = [];
    public IReadOnlyDictionary<string, IWebSiteInjectContentPart> Scripts => _scripts;

    string _compiledHeader = string.Empty;
    public string CompiledHeader
    {
        get
        {
            if (_invalidate) Compile();
            return _compiledHeader;
        }
    }

    string _compiledFooter = string.Empty;
    public string CompiledFooter
    {
        get
        {
            if (_invalidate) Compile();
            return _compiledFooter;
        }
    }
    private bool _invalidate;

    private void Compile()
    {
        _compiledHeader = _scripts.Values.Where(s => s.PlaceInHead).OrderBy(s => s.Order).Select(s => s.HtmlContent()).JoinStr("\n\t").Trim();
        _compiledFooter = _scripts.Values.Where(s => !s.PlaceInHead).OrderBy(s => s.Order).Select(s => s.HtmlContent()).JoinStr("\n\t").Trim();
        _invalidate = false;
    }

    public void AddScript(string name, IWebSiteInjectContentPart script)
    {
        _scripts.Add(name, script);
        _invalidate = true;
        siteScriptsBuilder.ClearCache();
    }

    public void RemoveScript(string name)
    {
        _scripts.Remove(name);
        _invalidate = true;
        siteScriptsBuilder.ClearCache();
    }
}
