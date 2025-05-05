using Mars.Host.Shared.Exceptions;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Host.Templator;

public class RegisterNodeTemplatorFunction
{
    public async static Task<object?> Node(IXTFunctionContext ctx)
    {

        string[] argsEl = ctx.Arguments;
        var key = ctx.Key;
        var val = ctx.Val;

        if (argsEl.Length != 1 && argsEl.Length != 2)
        {
            //ctx.PageContext.AddError($"Req arguments wrong argument count [2,3] given {argsEl.Length}: key=\"{key}\" val=\"{val}\"");
            throw new XTFunctionException($"Req arguments wrong argument count [2,3] given {argsEl.Length}: key=\"{key}\" val=\"{val}\"");
            return null;
        }

        INodeService nodeService = ctx.ServiceProvider.GetRequiredService<INodeService>();

        List<object> args = new();

        foreach (var a in argsEl)
        {
            var evalResult = ctx.Ppt.Get.Eval(a, ctx.Ppt.GetParameters());
            args.Add(evalResult);
        }

        string nodeName = args.First().ToString()!;
        object? payload = null;

        if (argsEl.Length == 2)
        {
            payload = args[1];
        }

        UserActionResult<object?> result = await nodeService.CallNode(ctx.ServiceProvider, nodeName, payload);

        //pctx.context.Add(key, result.Data);
        //ctx.ppt.parameters.Add(key, new Parameter(key, result.Data));

        return result?.Data;
    }
}
