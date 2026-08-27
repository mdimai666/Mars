namespace Mars.Host.Shared.Templators;

public interface IMarsHtmlTemplator : IDisposable
{
    public void RegisterContextFunctions();
    MarsHtmlTemplate<object, object> Compile(string template);
    public delegate string MarsHtmlTemplate<in TContext, in TData>(TContext context, TData? data = null) where TContext : class where TData : class;
    public void RegisterTemplate(string templateName, string template);
}

