using Mars.SiteEngine.Abstractions.WebSite.Models;

namespace Mars.SiteEngine.Abstractions.Templators;

/// <summary>
/// IXTFunctionContext
/// </summary>
/// <exception cref="XTFunctionException"/>
public interface IXTFunctionContext
{
    public IServiceProvider ServiceProvider { get; }
    public PageRenderContext PageContext { get; }
    public string Key { get; }
    public string Val { get; }
    public string[] Arguments { get; }

    public XInterpreter Ppt { get; }
}

public delegate Task<object?> TemplatorRegisterFunction(IXTFunctionContext ctx);
