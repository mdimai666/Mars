using Mars.Host.Shared.Exceptions;
using Mars.Host.Shared.Templators;
using Mars.Host.Shared.WebSite.Models;

namespace Mars.Host.Shared.Interfaces;

/// <summary>
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

