using Mars.Core.Features;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.WebSite.Models;

namespace Mars.Host.Shared.Templators;

public class XTFunctionContext : IXTFunctionContext
{
    public IServiceProvider ServiceProvider { get; }
    public PageRenderContext PageContext { get; }
    public string Key { get; }
    public string Val { get; }
    public string[] Arguments { get; }

    public XInterpreter Ppt { get; }
    public CancellationToken cancellationToken { get; }

    public XTFunctionContext(string key, string val, PageRenderContext pageContext, XInterpreter ppt, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        Key = key;
        Val = val;
        Arguments = TextHelper.ParseArguments(val);
        PageContext = pageContext;
        ServiceProvider = serviceProvider;
        Ppt = ppt;
    }

}
