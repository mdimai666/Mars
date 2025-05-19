using System.Text.Json.Nodes;
using Flurl.Http;
using Mars.Host.Shared.Exceptions;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Services;
using Mars.Shared.Templators;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Templators;

public class TemplatorRegisterFunctions
{

    public static Task<object> Paginator(IXTFunctionContext ctx)
    {
        var arguments = ctx.Arguments;
        var key = ctx.Key;
        var val = ctx.Val;

        var req = ctx.PageContext.Request;

        //TODO: тут порядок важен, и оно должно отработать последним
        PaginatorHelper paginator;
        //string argsStr = val.Substring("Paginator(".Length, val.Length - "Paginator(".Length - 1);


        string[] argsEl = arguments;

        //if (argsEl.Length is not 2 or 3)
        if (argsEl.Length != 2 && argsEl.Length != 3)
        {
            //ctx.PageContext.AddError($"Paginator arguments wrong argument count [2,3] (page?,total,pageSize) given {argsEl.Length}: key=\"{key}\" val=\"{val}\"");
            throw new XTFunctionException($"Paginator arguments wrong argument count [2,3] (page?,total,pageSize) given {argsEl.Length}: key=\"{key}\" val=\"{val}\"");
            return null;
        }

        List<int> args = new();

        int aIndex = 0;
        bool isBreaked = false;
        foreach (var a in argsEl)
        {
            if (int.TryParse(a, out int x))
            {
                args.Add(x);
            }
            else
            {
                var result = ctx.Ppt.Get.Eval<int>(a, ctx.Ppt.GetParameters());
                args.Add(result);
            }
            if (isBreaked) continue;
            aIndex++;
        }

        if (args.Count == 2)
        {
            int page = 1;
            if (req.QueryDict.ContainsKey("page"))
            {
                page = int.Parse(req.QueryDict["page"]);
            }
            paginator = new PaginatorHelper(page, args[0], args[1]);
            //pctx.context.Add(key, paginator);

        }
        else if (args.Count == 3)
        {
            paginator = new PaginatorHelper(args[0], args[1], args[2]);
            //pctx.context.Add(key, paginator);
        }
        else
        {
            //ctx.PageContext.AddError($"Paginator arguments wrong: key=\"{key}\" val=\"{val}\"");
            throw new XTFunctionException($"Paginator arguments wrong: key=\"{key}\" val=\"{val}\"");
            return null;
        }

        //if (ctx.ppt.parameters.ContainsKey(key)) ctx.ppt.parameters.Remove(key);
        //ctx.ppt.parameters.Add(key, new Parameter(key, paginator));

        return Task.FromResult<object>(paginator);

    }

    public async static Task<object> Req(IXTFunctionContext ctx)
    {
        //string argsStr = val.Substring("Req(".Length, val.Length - "Req(".Length - 1);

        var arguments = ctx.Arguments;
        var key = ctx.Key;
        var val = ctx.Val;


        //string[] argsEl = argsStr.Split(',');
        string[] argsEl = arguments;

        //if (argsEl.Length is not 2 or 3)
        if (argsEl.Length != 2 && argsEl.Length != 3)
        {
            //ctx.PageContext.AddError($"Req arguments wrong argument count [2,3] given {argsEl.Length}: key=\"{key}\" val=\"{val}\"");
            throw new XTFunctionException($"Req arguments wrong argument count [2,3] given {argsEl.Length}: key=\"{key}\" val=\"{val}\"");
            return null;
        }

        List<object> args = new();

        int aIndex = 0;
        foreach (var a in argsEl)
        {
            var result = ctx.Ppt.Get.Eval(a, ctx.Ppt.GetParameters());
            args.Add(result);
            aIndex++;
        }

        string method = args[0].ToString().ToUpper();
        string url = args[1].ToString()!;
        string? body = method != "GET" ? args[2].ToString() : null;

        using var http = ctx.ServiceProvider.GetRequiredService<IFlurlClient>();

        string res;
        JsonNode? res_json = null;
        if (method == "GET") res = await http.Request(url).GetStringAsync(); //TODO: CancellationToken
        else res = await http.Request(url).PostStringAsync(body).ReceiveString();

        //bool isObject = res.Length > 1 && (res[0] == '{' || res[0] == '[')
        bool isObject = res.Length > 1 && res[0] == '{';
        bool isArr = res.Length > 1 && res[0] == '[';

        if (isObject || isArr)
            res_json = JsonNode.Parse(res);

        object total = res_json is not null ? res_json : res;

        //if (isArr)
        //    pctx.context.Add(key, JArray.FromObject(total));
        //else
        //    pctx.context.Add(key, JToken.FromObject(total));

        //pctx.context.Add(key, total);

        //if (ctx.ppt.parameters.ContainsKey(key)) ctx.ppt.parameters.Remove(key);
        //ctx.ppt.parameters.Add(key, new Parameter(key, total));

        return total;
    }


    public static Task<object> CalendarRow(IXTFunctionContext ctx)
    {
        var startDate = DateTime.Now;
        int monthNum = startDate.Month;
        int monthCount = 2;

        CalendarRowInfo rowCalendar = new();

        rowCalendar.Months.Add(new CalendarMonthInfo(startDate));

        for (int mi = 1; mi < monthCount; mi++)
        {
            DateTime date = new DateTime(startDate.Year, startDate.Month, 1).AddMonths(mi);
            rowCalendar.Months.Add(new CalendarMonthInfo(date));
        }

        return Task.FromResult<object>(rowCalendar);
    }

    //TODO: add help description and #context function help text
    public static Task<object> Help(IXTFunctionContext ctx)
    {
        var locator = ctx.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>();
        return Task.FromResult<object>(locator.AllMetaRelationsStructure());
    }

}
