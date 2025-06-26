using DynamicExpresso;
using Mars.Host.Shared.WebSite.Models;

namespace Mars.Host.Shared.Templators;

public class XInterpreter
{
    Interpreter? _interpreter = null;

    protected readonly PageRenderContext? pageContext;
    protected readonly Dictionary<string, object>? contextForIterator;
    public Dictionary<string, Parameter> parameters { get; set; } = [];

    public Interpreter Get
    {
        get
        {
            _interpreter ??= initInterprer();
            return _interpreter;
        }
    }

    public XInterpreter()
    {

    }

    public XInterpreter(PageRenderContext? pageContext, Dictionary<string, object>? contextForIterator = null)
    {
        this.pageContext = pageContext;
        this.contextForIterator = contextForIterator;
    }

    Interpreter initInterprer()
    {
        var e = new Interpreter(InterpreterOptions.Default);

        if (contextForIterator is not null)
        {
            foreach (var v in contextForIterator)
            {
                e.SetVariable(v.Key, v.Value);
                if (v.Value is not null)
                {
                    parameters.Add(v.Key, new Parameter(v.Key, v.Value));
                }
            }
        }

        if (pageContext is not null)
        {
            e.SetVariable("this", pageContext.TemplateContextVaribles); // TODO: тут надо подумать куда ссылку оставлять

            foreach (var v in pageContext.TemplateContextVaribles)
            {
                if (!v.Key.StartsWith('$') && v.Value is not null)
                {
                    e.SetVariable(v.Key, v.Value);
                    parameters.TryAdd(v.Key, new Parameter(v.Key, v.Value));
                }
            }
        }

        return e;
    }

    public Parameter[] GetParameters()
    {
        _interpreter ??= initInterprer();
        return parameters.Select(s => s.Value).ToArray();
    }
}
