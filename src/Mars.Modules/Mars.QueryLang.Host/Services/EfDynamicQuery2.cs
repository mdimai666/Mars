using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Mars.Host.Data;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Templators;
using Mars.Host.Shared.WebSite.Models;
using Mars.Core.Features;
using DynamicExpresso;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Mars.Host.Shared.Dto.Profile;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Data.Common;

namespace Mars.Host.QueryLang;

/// <summary>
/// Still not user in Production
/// </summary>
[Obsolete]
public static class EfDynamicQueryHelper2
{
    public static object Query(string key, string val, int index, PageRenderContext pageRenderContext, XInterpreter ppt)
    {
        return new EfDynamicQuery2(null!).Query(key, val, index, pageRenderContext, ppt);
    }
}

[Obsolete]
public class EfDynamicQuery2(IServiceProvider serviceProvider)
{

    ////bool init = false;

    public object Query(string key, string val, int index, PageRenderContext renderContext, XInterpreter ppt)
    {
        //    //if (!init)
        //    //{
        //    //    init = true;

        //    //    EfDynQueryDict.RegisterFilter<QWhere, Post>("Where");
        //    //    EfDynQueryDict.RegisterGetter<QCount, Post, int>("Count");
        //    //}

        //    //Dictionary<string, object> context = pctx.context;

        //    //var serviceProvider = renderContext.ServiceProvider;

        //    //Action<string> addErr = renderContext.AddError;
        //    Action<string> addErr = s => { };
        //    MarsDbContext ef = serviceProvider.GetRequiredService<MarsDbContext>();
        //    //IPostService postService = pctx.GetService<IPostService>();
        //    //Post post = pctx.post;
        //    //PostType postType = pctx.postType;
        //    //var user = renderContext.PageContext._user;
        //    var user = renderContext.User;
        //    //WebClientRequest req = pctx.req;
        //    //QServer http = pctx.GetService<QServer>();
        //    //IUserService userService = pctx.GetService<IUserService>();
        //    IMetaModelTypesLocator mlocator = serviceProvider.GetRequiredService<IMetaModelTypesLocator>();

        //    var postTypes = ef.PostTypes.Include(s => s.MetaFields).ToList();
        //    var postTypesDict = postTypes.ToDictionary(s => s.TypeName);
        //    var postTypeNames = postTypes.Select(s => s.TypeName).ToList();

        //    var spl = val.Split('.', count: 2);
        //    string cmd = spl[0];
        //    var chain = TextHelper.ParseChainPairKeyValue(val);


        //    Type entityType = mlocator.GetModelType(Mars.Shared.Contracts.MetaFields.MetaFieldType.Bool, cmd);

        //    bool isPost = entityType == typeof(PostEntity);
        //    string postTypeName = cmd;
        //    //string expVarName = "post";

        //    mlocator.TryUpdateMetaModelMtoRuntimeCompiledTypes(serviceProvider);

        //    if (isPost && mlocator.MetaMtoModelsCompiledTypeDict.ContainsKey(postTypeName) == false)
        //    {
        //        addErr($"post type \"{postTypeName}\" not found");
        //        return null;
        //    }

        //    //IQueryable<TEntity> query = MarsDbContext.DbSetByType(entityType, ef, pctx.serviceProvider) as dynamic;

        //    //if (isPost && postTypeName != nameof(Post) && !postTypeNames.Contains(postTypeName))
        //    //{
        //    //    throw new ArgumentException($"postType \"{postTypeName}\" not found");
        //    //}

        //    //if (isPost && postTypeName != nameof(Post))
        //    //{
        //    //    query = query.Where(s => (s as Post).Type == postTypeName);
        //    //}
        //    //query = query.OrderByDescending(s => s.Created);

        //    //foreach (var x in chain)
        //    //{
        //    //    string func = x.Key;
        //    //    string[] args = string.IsNullOrEmpty(x.Value) ? new string[] { } : TextHelper.SplitArguments(x.Value);

        //    //    EfDynQueryDict.DQC_Context ctx = new EfDynQueryDict.DQC_Context
        //    //    {
        //    //        context = context,
        //    //        ef = ef,
        //    //        entityType = entityType,
        //    //        postTypes = postTypes,
        //    //        ppt = ppt
        //    //    };

        //    //    // TODO /!\ other PostTypes than Post

        //    //    if (EfDynQueryDict.ExecuteFilter<TEntity>(func, query, args, ctx, out var queryFiltered))
        //    //    {
        //    //        query = queryFiltered;
        //    //    }
        //    //    else if (EfDynQueryDict.ExecuteGetter<TEntity, int>(func, query, args, ctx, out int count))
        //    //    {
        //    //        var _context = count;
        //    //        context.Add(key, _context);
        //    //        ppt.parameters.Add(key, _context);
        //    //        return;
        //    //    }
        //    //    //--
        //    //    else
        //    //    {
        //    //        throw new ArgumentException($"Function \"{func}\" for T:\"{typeof(TEntity).Name}\" not found");
        //    //    }
        //    //}

        //    //var result = await query.ToListAsync();

        //    //if (context.ContainsKey(key)) context.Remove(key);
        //    //if (result.Count == 0)
        //    //{
        //    //    context.Add(key, JToken.FromObject(new string[] { }));

        //    //}
        //    //else
        //    //{
        //    //    var _context = JArray.FromObject(result);
        //    //    context.Add(key, _context);
        //    //    ppt.parameters.Add(key, _context);
        //    //}

        //    var qef = new EntityQuery(serviceProvider, ppt, user.Detail);

        //    var result = qef.Query(val);

        //    JsonSerializerSettings sett = new JsonSerializerSettings
        //    {
        //        Formatting = Formatting.None,
        //        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        //    };

        //    var jss = new JsonSerializer() { MaxDepth = 1, ReferenceLoopHandling = ReferenceLoopHandling.Ignore, Formatting = Formatting.None };

        //    //if (context.ContainsKey(key)) context.Remove(key);
        //    if (result is null)
        //    {
        //        //context.Add(key, null);
        //        return null;
        //        ////ppt.parameters.Add(key, result);
        //    }
        //    else if (result.GetType().IsPrimitive)
        //    {
        //        //context.Add(key, result);
        //        if (!ppt.parameters.ContainsKey(key))
        //        {
        //            ppt.parameters.Add(key, new Parameter(key, result));
        //        }
        //        return result;
        //    }
        //    else if (result is IEnumerable<IBasicEntity>)
        //    {
        //        //context.Add(key, JToken.FromObject(new string[] { }));
        //        //var _context = JArray.FromObject(result, jss);
        //        //context.Add(key, _context);
        //        if (!ppt.parameters.ContainsKey(key))
        //        {
        //            ppt.parameters.Add(key, new Parameter(key, result));
        //        }
        //        //ppt.parameters.Add(key, result);
        //        return result;
        //    }
        //    else
        //    {
        //        //var _context = JObject.FromObject(result, jss);
        //        //context.Add(key, _context);
        //        if (!ppt.parameters.ContainsKey(key))
        //        {
        //            ppt.parameters.Add(key, new Parameter(key, result));
        //        }
        //        return result;
        //        //ppt.parameters.Add(key, result);
        //    }
        throw new NotImplementedException();

    }

    //public Expression<Func<TEntity, bool>> parseExpression(XInterpreter ppt, Type entityType, string text, string varible = "post")
    //{
    //    return ppt.Get.ParseAsExpression<Func<TEntity, bool>>(text, varible);

    //}


}


