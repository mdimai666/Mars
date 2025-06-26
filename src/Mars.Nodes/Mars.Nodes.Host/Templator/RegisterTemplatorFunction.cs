using Mars.Host.Shared.Exceptions;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Templators;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Host.Templator;

public static class RegisterNodeTemplatorFunction
{
    [TemplatorHelperInfo("Node", """x = Node(callNodeName, payload? = null)""", "Вызов ноды CallNode с именем nodeName и передача ей необязательного параметра payload. Возвращает результат выполнения ноды.")]
    public static async Task<object?> Node(IXTFunctionContext ctx)
    {
        string[] argsEl = ctx.Arguments;
        var key = ctx.Key;
        var val = ctx.Val;

        if (argsEl.Length != 1 && argsEl.Length != 2)
        {
            throw new XTFunctionException($"Req arguments wrong argument count [2,3] given {argsEl.Length}: key=\"{key}\" val=\"{val}\"");
            return null;
        }

        var nodeService = ctx.ServiceProvider.GetRequiredService<INodeService>();

        var args = new List<object>();

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

        var result = await nodeService.CallNode(ctx.ServiceProvider, nodeName, payload);

        return result?.Data;
    }
}
