using System.Linq.Expressions;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Templators;
using Mars.QueryLang.Host.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace Mars.Host.QueryLang;

//experiments file
interface IQueryChainFilter
{

}

public abstract class QueryBase
{
    public EfDynQueryDict.DQC_Context ctx { get; set; }

}

public abstract class QueryChainFilter<T> : QueryBase, IQueryChainFilter where T : class, new()
{
    public abstract IQueryable<T> Execute(IQueryable<T> query, object[] args);

    public Expression<Func<T, bool>> parseExpression(string text, string varible = "post")
    {
        return ctx.ppt.Get.ParseAsExpression<Func<T, bool>>(text, varible);

    }
}



public abstract class QueryGetter<T, TResult> : QueryBase where T : class, new()
{
    public abstract TResult Execute(IQueryable<T> query, object[] args);

    public Expression<Func<T, bool>> parseExpression(string text, string varible = "post")
    {
        return ctx.ppt.Get.ParseAsExpression<Func<T, bool>>(text, varible);

    }
}

class QWhere : QueryChainFilter<PostEntity>
{
    public override IQueryable<PostEntity> Execute(IQueryable<PostEntity> query, object[] args)
    {
        MyThrowHelper.IfArgumentCount(args, 1);

        var expr = parseExpression(args[0] as string);

        return query.Where(expr);
    }
}

class QCount : QueryGetter<PostEntity, int>
{
    public override int Execute(IQueryable<PostEntity> query, object[] args)
    {
        MyThrowHelper.IfArgumentCount(args, 0, 1);

        if (args.Count() == 0)
        {
            return query.Count();
        }
        else
        {
            var expr = parseExpression(args[0] as string);
            return query.Count(expr);
        }
    }
}


public static class EfDynQueryDict
{
    static List<QDict> getters = new();
    static List<QDict> filters = new();

    class QDict
    {
        public string name;
        public Type type;
        public object instance;

        public QDict(string name, Type type, object instance)
        {
            this.name = name;
            this.type = type;
            this.instance = instance;
        }
    }

    public class DQC_Context
    {
        public XInterpreter ppt;
        public Type entityType;
        public JObject context;
        public MarsDbContext ef;
        public List<PostTypeEntity> postTypes;
        public IMetaModelTypesLocator mlocator;
        public string postTypeName;
        public IServiceProvider serviceProvider;
        public UserEntity? user;

        public Dictionary<string, PostTypeEntity> postTypesDict;

        public DQC_Context(IServiceProvider sp, string postTypeName, Type entityType,
            MarsDbContext ef, XInterpreter ppt, JObject pageContext, List<PostTypeEntity> postTypes,
            UserEntity? user)
        {
            this.serviceProvider = sp;
            this.postTypeName = postTypeName;
            this.entityType = entityType;
            this.ef = ef;
            this.ppt = ppt;
            this.context = pageContext;
            this.postTypes = postTypes;
            this.user = user;

            this.mlocator = sp.GetRequiredService<IMetaModelTypesLocator>();

            this.postTypesDict = postTypes.ToDictionary(s => s.TypeName);
        }

    }

    public static void RegisterFilter<TQueryChainFilter, T>(string name) where TQueryChainFilter : QueryChainFilter<T>, new() where T : class, new()
    {
        TQueryChainFilter instance = new();
        filters.Add(new(name, typeof(T), instance));
    }
    public static void RegisterGetter<TQueryGetter, T, TResult>(string name) where TQueryGetter : QueryGetter<T, TResult>, new() where T : class, new()
    {
        TQueryGetter instance = new();
        getters.Add(new(name, typeof(T), instance));
    }

    public static bool ExecuteFilter<T>(string name, IQueryable<T> query, object[] args, EfDynQueryDict.DQC_Context ctx, out IQueryable<T> result) where T : class, new()
    {
        var find = filters.FirstOrDefault(s => s.name == name && s.type == typeof(T));
        if (find == null)
        {
            result = default;
            return false;
        };
        var z = (find.instance as QueryChainFilter<T>);
        z.ctx = ctx;
        result = z.Execute(query, args);
        return true;
    }

    public static bool ExecuteGetter<T, TResult>(string name, IQueryable<T> query, object[] args, EfDynQueryDict.DQC_Context ctx, out TResult result) where T : class, new()
    {
        var find = getters.FirstOrDefault(s => s.name == name && s.type == typeof(T));
        if (find == null)
        {
            result = default;
            return false;
        };
        var z = (find.instance as QueryGetter<T, TResult>);
        z.ctx = ctx;
        result = z.Execute(query, args);
        return true;

    }
}

