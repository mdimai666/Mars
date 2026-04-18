using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Mars.SemanticKernel.Host.Plugins;

public sealed class ToolLoggingFilter : IFunctionInvocationFilter
{
    private readonly ILogger<ToolLoggingFilter> _logger;

    public ToolLoggingFilter(ILogger<ToolLoggingFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, Task> next)
    {
        var fn = context.Function;

        _logger.LogInformation(
            "🔧 Tool call: {Plugin}.{Function} args={Args}",
            fn.PluginName,
            fn.Name,
            context.Arguments.ToDictionary(k => k.Key, v => v.Value)
        );

        var sw = Stopwatch.StartNew();

        try
        {
            await next(context);

            _logger.LogInformation(
                "✅ Tool result: {Plugin}.{Function} ({Elapsed} ms)\n{Result}",
                fn.PluginName,
                fn.Name,
                sw.ElapsedMilliseconds,
                context.Result?.ToString()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Tool error: {Plugin}.{Function}",
                fn.PluginName,
                fn.Name
            );
            throw;
        }
    }

}
