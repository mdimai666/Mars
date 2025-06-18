using System.Text.RegularExpressions;
using DynamicExpresso;
using Mars.Core.Features;
using Mars.Host.Shared.QueryLang.Services;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Templators;
using Mars.Host.Shared.WebSite.Models;

namespace Mars.QueryLang.Host.Services;

public class QueryLangProcessing(
    ITemplatorFeaturesLocator tflocator,
    IServiceProvider serviceProvider,
    IQueryLangLinqDatabaseQueryHandler queryLangLinqDatabaseQueryHandler) : IQueryLangProcessing
{
    private static readonly Regex regexLinqFunctionDetect = new Regex(@"^\w+\.\w+");
    private static readonly Regex regexFunctionDetect = new Regex(@"^\w+\(");

    public async Task<Dictionary<string, object?>> Process(
        PageRenderContext pageContext,
        IReadOnlyCollection<KeyValuePair<string, string>> Queries,
        Dictionary<string, object>? localVaribles,
        CancellationToken cancellationToken)
    {
        XInterpreter ppt = new(pageContext, localVaribles);

        var functions = tflocator.Functions;

        int index = 0;
        string processKey = null!;

        //Action<string> addErr = err => renderContext.PageContext.Errors.Add(err);
        Action<string> addErr = err => { };

        Dictionary<string, object?> resultDict = new();

        Action<string, object?> addToContext = (string key, object? value) =>
        {
            if (key != "_" && value != null)
            {
                if (ppt.parameters.ContainsKey(key)) ppt.parameters.Remove(key);
                ppt.parameters.Add(key, new Parameter(key, value));
                ppt.Get.SetVariable(key, value);

                resultDict.Add(key, value);
            }
        };

        try
        {
            foreach (var _x in Queries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processKey = _x.Key;
                string key = _x.Key;
                string val = _x.Value;

                if (string.IsNullOrWhiteSpace(val)) continue;

                if (string.IsNullOrWhiteSpace(key))
                {
                    addErr($"$context => \"key\" must be set (index:{index})");
                    continue;
                }

                if (val.Length < 2)
                {
                    addErr($"$context => \"length\" minimum: 2 (index:{index})");
                    continue;
                }

                bool isObjectFunc = regexLinqFunctionDetect.IsMatch(val);
                bool isFunction = regexFunctionDetect.IsMatch(val);

                string? funcName = isFunction ? val.Split('(', 2).First() : null;

                //dynamic!
                if (val.StartsWith("//") || key.StartsWith("//"))
                {
                    continue;
                }
                else if (val[0] == '=')
                {
                    string ex = val.Substring(1);

                    //var result = ppt.Get.Eval("8 / 2 + 2");
                    var vaa = ppt.GetParameters();
                    var result = ppt.Get.Eval(ex, ppt.GetParameters());

                    addToContext(key, result);

                    //TODO: WARNING =company?.id.ToString()+"xxx" not work
                }
                else if (isFunction && funcName is not null && functions.TryGetValue(funcName, out var ff))
                {
                    var pairs = TextHelper.ParseArguments(val);
                    string[] arguments = pairs;
                    //XTFunctionContext ctx = new() { pctx = pctx, key = key, val = val, ppt = ppt, arguments = arguments };

                    XTFunctionContext ctx = new(key, val, pageContext, ppt, serviceProvider, cancellationToken);
                    var result = await ff(ctx);

                    if (result is not null)
                    {
                        addToContext(key, result);
                    }
                }
                else if (isObjectFunc)
                {
                    //var result = EfDynamicQueryHelper2.Query(key, val, index, renderContext, ppt);
                    var providerObject = val.Split('.', 2)[0];
                    if (providerObject == "ef")
                    {
                        var result = await queryLangLinqDatabaseQueryHandler.Handle(val.Substring(3), ppt, cancellationToken);
                        addToContext(key, result);
                    }
                    else
                    {
                        throw new NotImplementedException($"context object '{providerObject}' not implement");
                    }
                    //await EfDynamicQueryHelper.Query(key, val, index, pctx, ppt);
                }
                else
                {
                    addErr($"on $context <b>\"{processKey}\"</b>: value= {val} not implement.");
                }

                index++;
            }
        }

        catch (Exception ex)
        {
#if DEBUG
            addErr($"on add $context error <b>\"{processKey}\"</b>: {ex.Message}<br><pre>trace:\n{ex.StackTrace.ReplaceLineEndings("<br/>")}</pre>");
#else
            addErr($"on add $context error <b>\"{processKey}\"</b>: {ex.Message}");
#endif
        }

        return resultDict;
    }
}
