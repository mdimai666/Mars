using System.Text.RegularExpressions;
using Mars.Host.Data;
using Mars.Host.Services;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Templators;
using Mars.Core.Extensions;
using Mars.Core.Features;
using DynamicExpresso;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Mars.Host.Shared.Dto.Profile;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Data.Contexts;

namespace Mars.Host.Templators;

public class TemplatorContextPreparator
{

    [Obsolete]
    public static void PrepareContext(object pctx, Dictionary<string, object> contextForIterator = null)
    {
        throw new NotImplementedException();

#if false

        Dictionary<string, object> context = pctx.context;

        Action<string> addErr = pctx.addErr;
        MarsDbContext ef = pctx.ef;
        IPostService postService = pctx.serviceProvider.GetRequiredService<IPostService>();
        PostDetail post = pctx.post;
        PostTypeDetail postType = pctx.postType;
        UserDetail user = pctx.user;
        WebClientRequest req = pctx.req;
        QServer http = pctx.serviceProvider.GetService<QServer>();


        string context_key = "context";


        if (!context.ContainsKey(context_key)) return;

        string processKey = null;

        try
        {
            Dictionary<string, string> ctxList = new();

            //var ctxList = context[context_key] as IEnumerable<KeyValuePair<string, string>>;

            if (context[context_key] is IEnumerable<KeyValuePair<string, string>> enumer)
            {
                foreach (var a in enumer)
                {
                    ctxList.Add(a.Key, a.Value);
                }
            }
            else if (context[context_key] is Dictionary<string, string> dict)
            {
                foreach (var a in dict)
                {
                    ctxList.Add(a.Key, a.Value);
                }
            }
            else if (context[context_key] is JArray jarr)
            {
                foreach (JObject a in jarr)
                {
                    ctxList.Add(a["key"].ToString(), a["value"].ToString());
                }
            }
            else
            {
                throw new NotImplementedException($"#context is not implement type ({context[context_key]?.GetType()})");
            }

            if (ctxList.Count() == 0) return;

            //XInterpreter ppt = new(pctx, contextForIterator);
            XInterpreter ppt = null; throw new NotImplementedException();


#pragma warning disable CS0162 // Unreachable code detected
            foreach (var v in context)
            {
                ppt.parameters.Add(v.Key, new Parameter(v.Key, v.Value));
            }

            ITemplatorFeaturesLocator tflocator = pctx.serviceProvider.GetRequiredService<ITemplatorFeaturesLocator>();

            var functions = tflocator.Functions;

            //functions.Add(nameof(TemplatorRegisterFunctions.Paginator), TemplatorRegisterFunctions.Paginator);
            //functions.Add(nameof(TemplatorRegisterFunctions.Req), TemplatorRegisterFunctions.Req);
            //functions.Add(nameof(TemplatorRegisterFunctions.Node), TemplatorRegisterFunctions.Node);


            int index = 0;
            //ContextListComparet comparet = new();
            foreach (var _x in ctxList)
            {
                var e = _x;
                processKey = e.Key;
                string key = e.Key;
                string val = e.Value;

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


                //https://github.com/zzzprojects/Eval-Expression.NET#whats-eval-expressionnet
                //+ https://github.com/dynamicexpresso/DynamicExpresso


                //string _val = "=type+Convert.ToString(100)";
                //string _val = "Post.First(post.Type==\"page\")";
                //string _val = "Post.Where(post.Type==\"page\")";
                //string _val = "Post.Where(post.Type==\"post\")";

                Regex reObFu = new Regex(@"^\w+\.\w+");
                Regex reFunc = new Regex(@"^\w+\(");

                bool isObjectFunc = reObFu.IsMatch(val);
                bool isFunction = reFunc.IsMatch(val);

                string funcName = isFunction ? val.Split('(', 2).First() : null;

                //dynamic!
                if (val.StartsWith("//"))
                {
                    continue;
                }
                else if (val[0] == '=')
                {
                    string ex = val.Substring(1);

                    //var result = ppt.Get.Eval("8 / 2 + 2");
                    var vaa = ppt.GetParameters();
                    var result = ppt.Get.Eval(ex, ppt.GetParameters());

                    if (context.ContainsKey(key)) context.Remove(key);
                    context.Add(key, result);

                    if (ppt.parameters.ContainsKey(key)) ppt.parameters.Remove(key);
                    ppt.parameters.Add(key, new Parameter(key, result));
                    ppt.Get.SetVariable(key, result);


                    //TODO: WARNING =company?.id.ToString()+"xxx" not work
                }
                else if (isFunction && functions.ContainsKey(funcName))
                {
                    var pairs = TextHelper.ParseArguments(val);
                    string[] arguments = pairs;
                    //XTFunctionContext ctx = new() { pctx = pctx, key = key, val = val, ppt = ppt, arguments = arguments };

                    throw new NotImplementedException();
                    //XTFunctionContext ctx = new(key, val, pctx, ppt);
                    //var ff = functions[funcName];
                    //var result = await ff(ctx);

                    //if (result is not null)
                    //{
                    //    context.Add(key, result);
                    //    ppt.parameters.Add(key, new Parameter(key, result));
                    //    ppt.Get.SetVariable(key, result);
                    //}
                }
                else if (isObjectFunc)
                {
                    throw new NotImplementedException();
                    //await EfDynamicQueryHelper2.Query(key, val, index, pctx, ppt);
                    ////await EfDynamicQueryHelper.Query(key, val, index, pctx, ppt);
                }
                else
                {
                    addErr($"on $context <b>\"{processKey}\"</b>: value= {val} not implement.");
                }

                index++;
            }
#pragma warning restore CS0162 // Unreachable code detected

        }
        catch (Exception ex)
        {
#if DEBUG
            addErr($"on add $context error <b>\"{processKey}\"</b>: {ex.Message}<br><pre>trace:\n{ex.StackTrace.ReplaceLineEndings("<br/>")}</pre>");
#else
            addErr($"on add $context error <b>\"{processKey}\"</b>: {ex.Message}");
#endif
        }
#endif

    }

}

public class WebSiteTypesInfo
{
    public string[] DbSetTypes { get; }
    public string[] SingleItemCommands { get; }
    public string[] ManyItemCommands { get; }
    public IEnumerable<string> PostTypeNames { get; }
    public IEnumerable<string> KnownTypes { get; }

    public WebSiteTypesInfo(IEnumerable<string> postTypes)
    {
        throw new NotImplementedException();
        //DbSetTypes = new string[] {
        //    nameof(Post), nameof(User), nameof(NavMenu), nameof(FileEntity),
        //    nameof(PostCategory), nameof(PostType)
        //};
        //SingleItemCommands = new string[] { "First", "Last", "Count", "TakeRandom", "Meta" };
        ////string[] allowFunc = { "Where", "Take", "Skip", "Select", "Include", "ThenInclude" };
        //ManyItemCommands = new string[] { "Where", "Take", "Skip", "TakeRandomRange" };

        ////var postTypeNames = ef.PostTypes.Select(s => s.TypeName).ToList();
        //PostTypeNames = postTypes;
        //KnownTypes = PostTypeNames.Concat(DbSetTypes);
    }

    public bool IsOneQuery(string contextValue)
    {
        var joinedTypes = KnownTypes.JoinStr("|");
        var oneQuery1 = $"^({joinedTypes})\\.({SingleItemCommands.JoinStr("|")})";

        Regex re = new Regex(oneQuery1);
        return re.IsMatch(contextValue);
    }

    public bool IsManyQuery(string contextValue)
    {
        var joinedTypes = KnownTypes.JoinStr("|");
        var manyQuery1 = $"^({joinedTypes})\\.({ManyItemCommands.JoinStr("|")})";

        Regex re = new Regex(manyQuery1);
        return re.IsMatch(contextValue);
    }
}
