#if !true

using Mars.Host.Services;
using Mars.Host.Templators;
using Mars.Host.Templators.HandlebarsFunc;
using Mars.Core.Extensions;
using Mars.Core.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Nodes;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Templators;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Data.Common;
using Mars.Host.Data.Entities;
using Mars.Shared.Common;
using Mars.Shared.Templators;

namespace Mars.Host.QueryLang;

public class DefaultEfQueries : IDefaultEfQueries<IBasicEntity>
{
    public EfDynQueryDict.DQC_Context ctx;
    public IQueryable<IBasicEntity> query;

    public IQueryable<IBasicEntity> baseQuery;

    public Type ElementType => query.ElementType;
    public Expression Expression => query.Expression;
    public IQueryProvider Provider => query.Provider;

    Type _currentQueryEntityType = null;
    Type CurrentQueryEntityType
    {
        get => _currentQueryEntityType ?? ctx.entityType;
    }

    protected bool CurrentQueriGetSingle = false;
    protected bool CurrentQueriGetTable = false;

    protected TotalResponse2<IBasicEntity> _tableResponse;

    public Expression<Func<IBasicEntity, bool>> ParseExp<IBasicEntity>(string exp, string varName = "post")
    {
        return ctx.ppt.Get.ParseAsExpression<Func<IBasicEntity, bool>>(exp, varName);
    }

    public Expression<Func<TSource, TKey>> ParseKeySelector<TSource, TKey>(string exp, string varName = "post")
    {
        return ctx.ppt.Get.ParseAsExpression<Func<TSource, TKey>>(exp, varName);
    }

    LambdaExpression ParseExpA(string exp, string varName = "post")
    {
        MethodInfo method = this.GetType().GetMethod(nameof(this.ParseExp), BindingFlags.Public | BindingFlags.Instance, new Type[] { typeof(string), typeof(string) });
        method = method.MakeGenericMethod(CurrentQueryEntityType);
        return method.Invoke(this, new[] { exp, varName }) as LambdaExpression;
    }

    LambdaExpression ParseKeySelectorA(string propertyOrFieldName, string varName = "post")
    {
        //string propertyOrFieldName = "Title";
        //var z = ctx.ppt.Get.>(exp, varName);

        var elementType = CurrentQueryEntityType;
        var parameterExpression = Expression.Parameter(elementType);
        var propertyOrFieldExpression = Expression.PropertyOrField(parameterExpression, propertyOrFieldName);

        var selector = Expression.Lambda(propertyOrFieldExpression, parameterExpression);

        return selector;

        //MethodInfo method = this.GetType().GetMethod(nameof(this.ParseKeySelector), BindingFlags.Public | BindingFlags.Instance, new Type[] { typeof(string), typeof(string) });
        //method = method.MakeGenericMethod(ctx.entityType, keyType);
        //return method.Invoke(this, new[] { exp, varName });
    }

    object? CallQueryableMethod(string methodName, string expr)
    {
        var exp = ParseExpA(expr);

        ////var _method = typeof(Queryable)
        ////      .GetMethods(BindingFlags.Static | BindingFlags.Public)
        ////      //narrow the search before doing 'Single()'
        ////      .Where(mi => mi.Name == methodName
        ////                 // this check technically not required, but more future proof
        ////                 && mi.IsGenericMethodDefinition
        ////                 && mi.GetParameters().Length == 2);
        ////.MakeGenericMethod(ctx.entityType);

        ////Expression.Lambda()

        //var fet = typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(ctx.entityType, typeof(bool)));

        ////var fet2 = typeof(Expression<>)
        ////            .MakeGenericType(typeof(Func<,>)
        ////                .MakeGenericType(typeof(IBasicEntity), typeof(bool))
        ////            );

        //var xt = exp.GetType();

        //bool eq = fet == xt.BaseType;


        MethodInfo method = typeof(Queryable)
              .GetMethods(BindingFlags.Static | BindingFlags.Public)
              //narrow the search before doing 'Single()'
              .First(mi => mi.Name == methodName
                         // this check technically not required, but more future proof
                         && mi.IsGenericMethodDefinition
                         && mi.GetParameters().Length == 2
                         && mi.GetParameters()[1].Name == "predicate")
              //.Single(mi =>
              //{
              //    if (mi.Name == methodName
              //      && mi.IsGenericMethodDefinition
              //      && mi.GetParameters().Length == 2)
              //    {
              //        var pa = mi.GetParameters()[1];
              //        var z = 1;
              //    }

              //    return mi.Name == methodName
              //      && mi.IsGenericMethodDefinition
              //      && mi.GetParameters().Length == 2;
              //})
              .MakeGenericMethod(CurrentQueryEntityType);

        //&& mi.GetParameters()[1].ParameterType == typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(ctx.entityType, typeof(bool))))

        //var qType = typeof(IQueryable<>).MakeGenericType(ctx.entityType);

        //MethodInfo method = typeof(Queryable).GetMethod(
        //    methodName, BindingFlags.Public | BindingFlags.Static, 
        //    new Type[] { (qType).GetType(), exp.GetType() })
        //    .MakeGenericMethod(ctx.entityType);

        object? result = method.Invoke(query, new object[] { query, exp });

        return result;
    }


    public Dictionary<string, MethodInfo> MethodsMapping()
    {
        var methods = typeof(DefaultEfQueries)
              .GetMethods(BindingFlags.Instance | BindingFlags.Public)
              .Where(mi => mi.GetParameters().Length == 1
                         && mi.GetParameters()[0].ParameterType == typeof(string));

        return methods.ToDictionary(s => s.Name);
    }


    public int Count(string expr = "")
    {
        if (string.IsNullOrEmpty(expr))
            return query.Count();

        var result = CallQueryableMethod(nameof(Count), expr) as int?;

        return result.Value;

    }

    public IBasicEntity First(string expr = "")
    {
        CurrentQueriGetSingle = true;
        if (string.IsNullOrEmpty(expr))
        {
            Take(1);
            return query.FirstOrDefault();
        }

        //var exp = ParseExp<IBasicEntity>(expr);
        //return query.First(exp);

        var result = CallQueryableMethod(nameof(Queryable.FirstOrDefault), expr);
        Where(expr);
        Take(1);

        return result as IBasicEntity;
    }

    public IBasicEntity Last(string expr = "")
    {
        CurrentQueriGetSingle = true;
        if (string.IsNullOrEmpty(expr))
        {
            query = query.Reverse().Take(1);
            //Take(1);
            return query.LastOrDefault();
        }

        //var exp = ParseExp<IBasicEntity>(expr);
        //return query.Last(exp);

        var result = CallQueryableMethod(nameof(Queryable.LastOrDefault), expr);
        Where(expr);
        query = query.Reverse().Take(1);
        //query = Take(1);

        return result as IBasicEntity;
    }


    object? CallQueryableKeySelMethod(string methodName, string fieldName, string paramName)
    {
        var exp = ParseKeySelectorA(fieldName);

        MethodInfo method = typeof(Queryable)
              .GetMethods(BindingFlags.Static | BindingFlags.Public)
              .First(mi => mi.Name == methodName
                         // this check technically not required, but more future proof
                         && mi.IsGenericMethodDefinition
                         && mi.GetParameters().Length == 2
                         && mi.GetParameters()[1].Name == paramName)
              .MakeGenericMethod(CurrentQueryEntityType, exp.ReturnType);

        object? result = method.Invoke(query, new object[] { query, exp });

        return result;
    }

    public IDefaultEfQueries<IBasicEntity> OrderBy(string fieldName)
    {
        var result = CallQueryableKeySelMethod(nameof(Queryable.OrderBy), fieldName, "keySelector");

        query = result as IQueryable<IBasicEntity>;

        return this;
    }
    public IDefaultEfQueries<IBasicEntity> OrderByDescending(string fieldName)
    {
        var result = CallQueryableKeySelMethod(nameof(Queryable.OrderByDescending), fieldName, "keySelector");

        query = result as IQueryable<IBasicEntity>;

        return this;
    }

    public IDefaultEfQueries<IBasicEntity> Skip(int count)
    {
        query = query.Skip(count);

        return this;
    }

    public IDefaultEfQueries<IBasicEntity> Skip(string expr)
    {
        int count = ctx.ppt.Get.Eval<int>(expr);

        query = query.Skip(count);

        return this;
    }

    public IDefaultEfQueries<IBasicEntity> Take(int count)
    {
        query = query.Take(count);

        return this;
    }

    public IDefaultEfQueries<IBasicEntity> Take(string expr)
    {
        int count = ctx.ppt.Get.Eval<int>(expr);

        query = query.Take(count);

        return this;
    }

    public IDefaultEfQueries<IBasicEntity> Where(string expr)
    {
        //var exp = ParseExp<IBasicEntity>(expr);
        //return query.Where(exp);

        string _expr = expr;

        if (_expr.StartsWith('='))
        {
            var a = expr.Substring(1, expr.Length - 1);
            _expr = ctx.ppt.Get.Eval<string>(a);
        }

        var result = CallQueryableMethod(nameof(Queryable.Where), _expr);

        query = result as IQueryable<IBasicEntity>;

        return this;
    }

    public object Select(string expr)
    {
        //var exp = ParseExpA(expr);

        var fieldName = expr;

        var result = CallQueryableKeySelMethod(nameof(Queryable.Select), fieldName, "selector");

        //query = result as IQueryable<IBasicEntity>;

        return result;
    }

    public IDefaultEfQueries<IBasicEntity> Include(string expr)
    {

        //var exp = ParseExpA(expr);

        var navigationPropertyArg = ctx.ppt.Get.Eval<string>(expr);

        var methodName = nameof(EntityFrameworkQueryableExtensions.Include);

        MethodInfo method = typeof(EntityFrameworkQueryableExtensions)
              .GetMethods(BindingFlags.Static | BindingFlags.Public)
              .First(mi => mi.Name == methodName
                         // this check technically not required, but more future proof
                         && mi.IsGenericMethodDefinition
                         && mi.GetParameters().Length == 2
                         && mi.GetParameters()[1].ParameterType == typeof(string))
              .MakeGenericMethod(ctx.entityType);


        foreach (var navigationProperty in navigationPropertyArg.Split(','))
        {

            object? result = method.Invoke(query, new object[] { query, navigationProperty.Trim() });
            query = result as IQueryable<IBasicEntity>;
        }


        return this;
    }

    public IDefaultEfQueries<IBasicEntity> AsType(string postTypeName)
    {
        Type mtoType = ctx.mlocator.MetaMtoModelsCompiledTypeDict[postTypeName];

        var selectExpression = mtoType.GetField("selectExpression", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).GetValue(null);

        MethodInfo method = typeof(Queryable)
              .GetMethods(BindingFlags.Static | BindingFlags.Public)
        //narrow the search before doing 'Single()'
              .First(mi => mi.Name == nameof(Queryable.Select)
                         // this check technically not required, but more future proof
                         && mi.IsGenericMethodDefinition
                         && mi.GetParameters().Length == 2
                && mi.GetParameters()[1].Name == "selector")
              .MakeGenericMethod(typeof(PostEntity), mtoType);

        var result = method.Invoke(query, new object[] { query, selectExpression });

        query = result as IQueryable<IBasicEntity>;
        _currentQueryEntityType = mtoType;

        return this;
    }

    //public IDefaultEfQueries<IBasicEntity> Fill__oldvariant(string fillfields = "")
    public object Fill(string fillfields = "")
    {
        //bool isMto = _currentQueryEntityType.GetInterfaces().Contains(typeof(IMtoMarker));
        //bool isDto = _currentQueryEntityType.GetInterfaces().Contains(typeof(IDtoMarker));
        //if (isMto)
        //{
        //    throw new NotImplementedException("Only for pure Post allowed");
        //}
        //else if (isDto)
        //{
        //    throw new NotImplementedException("Only for pure Post allowed");
        //}
        //else 
        if (CurrentQueryEntityType.IsSubclassOf(typeof(PostEntity)))
        {
            if (string.IsNullOrEmpty(ctx.postTypeName))
            {
                throw new Exception("can fill only non direct Post type");
            }
            else
            {
                var result = FillData(fillfields);
                if (CurrentQueriGetSingle)
                {
                    return ((IEnumerable<IBasicEntity>)result).FirstOrDefault();
                }
                if (CurrentQueriGetTable)
                {
                    _tableResponse.Records = result as List<IBasicEntity>;
                    return _tableResponse;
                }
                else
                {
                    return result;
                }

            }
        }
        else
        {
            throw new NotImplementedException("Fill method allowed only for Post{Mto,Dto}");
        }

        throw new NotImplementedException("");
    }

    //TODO: test include with .baseQuery

    object FillData(string fillfields = "")
    {
        if (query.Count() == 0) return query;

        Type entityType = ctx.entityType;
        string typeName = ctx.postTypeName;
        var mlocator = ctx.mlocator;
        string[] _fillfields = fillfields.Split(',').Select(s => s.Trim()).ToArray();

        //gen dict
        var ids = query.Select(s => s.Id).ToList();
        var metaValues = ctx.ef.PostMetaValues
            .AsNoTracking()
            .Include(s => s.Post)
            .Include(s => s.MetaValue)
                .ThenInclude(s => s.MetaField)
            .Where(s => s.MetaValue.ModelId != Guid.Empty && ids.Contains(s.PostId))
            .Where(s => fillfields == "" || _fillfields.Contains(s.MetaValue.MetaField.Key))
            .Select(s => s.MetaValue)
            .ToList();


        Dictionary<Guid, MetaRelationObjectDict> fillDict = MyHandlebars.FillData(metaValues, ctx.serviceProvider);

        if (fillDict.Count > 0)
        {
            var list = fillDict.Select(s => s.Value).ToList();
            var grouped = list.GroupBy(s => mlocator.GetModelType(s.Type, s.ModelName));

            foreach (var group in grouped)
            {
                Type t = group.Key;
                EntityQuery eq = new EntityQuery(ctx.serviceProvider, ctx.ppt, ctx.user);

                var __ids = group.Select(s => s.ModelId);

                ctx.ppt.Get.SetVariable(nameof(__ids), __ids);

                IQueryable<IBasicEntity> items_query;



                items_query = eq.Query($"{group.First().ModelName}.Where({nameof(__ids)}.Contains(post.Id))") as IQueryable<IBasicEntity>;

                var items = items_query.ToList();

                foreach (var a in items)
                {
                    fillDict[a.Id].Entity = a;
                }

                ctx.ppt.Get.UnsetVariable(nameof(__ids));
            }
        }

        //--------------
        //fill fields
        string postDtoTypeName = ctx.postTypeName + "_dto";
        //query = baseQuery;
        //ctx.ppt.Get.SetVariable("__ids", ids);
        //AsType(postDtoTypeName);
        //Where("__ids.Contains(post.Id)");
        //ctx.ppt.Get.UnsetVariable("__ids");

        //return this;

        Type dtoType = ctx.mlocator.MetaMtoModelsCompiledTypeDict[postDtoTypeName];
        Type mtoType = ctx.mlocator.MetaMtoModelsCompiledTypeDict[typeName];
        //Type mtoType = ctx.mlocator.MetaMtoModelsCompiledTypeDict[postDtoTypeName];
        ConstructorInfo ctor = dtoType.GetConstructor(new[] { mtoType, typeof(EfQueryFillContext) });

        List<IBasicEntity> _list = new();



        PostType? postType = null;
        Dictionary<string, PostStatus>? statusDict = null;

        //bool? isLikeSupport = false;

        foreach (var a in query)
        {
            if (a is Post p && postType?.TypeName != p.Type)
            {
                postType = null;
                statusDict = null;
                //isLikeSupport = null;
            }

            //postType = ctx.ef.PostTypes.FirstOrDefault(s => s.TypeName == typeName);
            postType ??= ctx.postTypesDict[typeName];
            //isLikeSupport = postType.EnabledFeatures.Contains(nameof(Post.Likes));

            EfQueryFillContext fillContext = new EfQueryFillContext(fillDict, postType, ctx.user);

            //object instance = ctor.Invoke(new object[] { a, fillDict });
            object instance = ctor.Invoke(new object[] { a, fillContext });
            _list.Add(instance as IBasicEntity);
            if (instance is IMtoStatusProp st)
            {
                statusDict ??= postType.PostStatusList.ToDictionary(s => s.Slug);
                string? status = ((Post)instance).Status;
                if (string.IsNullOrEmpty(status) == false && statusDict.TryGetValue(status, out var postStatus))
                {
                    st.PostStatus = postStatus;
                }
            }
            if (ctx.user is not null && instance is IMtoLikes likeblePost)
            {
                //var likes = likeblePost.Likes;
                likeblePost.IsLiked = likeblePost.Likes.Any(s => s.UserId == ctx.user.Id);
            }
        }



        //query = list.AsQueryable();
        return _list;

    }

    public object FillTmp(string fillfields = "")
    {
        var ps = ctx.serviceProvider.GetRequiredService<IPostJsonService>();
        var us = ctx.serviceProvider.GetRequiredService<IUserService>();

        var ids = query.Select(s => s.Id).ToList();
        var posts = ctx.ef.Posts
            .AsNoTracking()
            .Include(s => s.MetaValues)
                .ThenInclude(s => s.MetaField)
            .Include(s => s.User)
            .Where(p => ids.Contains(p.Id))
            .ToList();

        JArray obj = null;
        if (posts.Count > 0)
        {
            obj = new JArray();
            string typeName = ctx.postTypeName;
            AppShared.Models.PostType postType = ctx.ef.PostTypes
                   .Include(s => s.MetaFields)
                   .AsNoTracking()
                   .FirstOrDefault(s => s.TypeName == typeName);

            foreach (var post in posts)
            {
                MfPreparePostContext pctx = new MfPreparePostContext
                {
                    ef = ctx.ef,
                    post = post,
                    postType = postType,
                    user = null,
                    userMetaFields = us.UserMetaFields(ctx.ef).ToList()
                };
                //obj = MfPreparePostContext.AsJson2(ref pctx, post.MetaValues, postType.MetaFields, ctx.serviceProvider);
                //MfPreparePostContext.AsJson2(ref pctx, post.MetaValues, postType.MetaFields, ctx.serviceProvider);
                obj.Add(ps.AsJson22(pctx));
            }
        }

        if (CurrentQueriGetSingle)
        {
            return obj != null ? obj.First() : obj;
        }
        else
        {
            return obj;
        }
    }

    IDefaultEfQueries<IBasicEntity> FillData_oldversion(string fillfields = "")
    {
        ////work
        //query = baseQuery;
        //AsType(ctx.postTypeName);
        //return this;
        ////work

        if (query.Count() == 0) return query as IDefaultEfQueries<IBasicEntity>;

        //IQueryable<IBasicEntity> query0 = query;

        var entityType = ctx.entityType;
        var typeName = ctx.postTypeName;
        var mlocator = ctx.mlocator;
        string[] _fillfields = fillfields.Split(',').Select(s => s.Trim()).ToArray();

        //there check meta fields

        var ids = query.Select(s => s.Id).ToList();
        var metaValues = ctx.ef.PostMetaValues
            .Include(s => s.Post)
            .Include(s => s.MetaValue)
            //.ThenInclude(s=>s.MetaField)
            .Where(s => s.MetaValue.ModelId != Guid.Empty && ids.Contains(s.PostId))
            .Where(s => fillfields == "" || _fillfields.Contains(s.MetaValue.MetaField.Key))
            .Select(s => s.MetaValue)
            .ToList();


        Dictionary<Guid, MetaRelationObjectDict> fillDict = MyHandlebars.FillData(metaValues, ctx.serviceProvider);

        if (fillDict.Count > 0)
        {
            var list = fillDict.Select(s => s.Value).ToList();
            var grouped = list.GroupBy(s => mlocator.GetModelType(s.Type, s.ModelName));

            foreach (var group in grouped)
            {
                Type t = group.Key;
                EntityQuery eq = new EntityQuery(ctx.serviceProvider, ctx.ppt, ctx.user);

                var __ids = group.Select(s => s.ModelId);

                ctx.ppt.Get.SetVariable(nameof(__ids), __ids);

                IQueryable<IBasicEntity> items_query;



                items_query = eq.Query($"{group.First().ModelName}.Where({nameof(__ids)}.Contains(post.Id))") as IQueryable<IBasicEntity>;

                var items = items_query.ToList();

                foreach (var a in items)
                {
                    fillDict[a.Id].Entity = a;
                }

                ctx.ppt.Get.UnsetVariable(nameof(__ids));
            }
        }


        //test
        //query = baseQuery;
        //AsType(ctx.postTypeName);
        //return this;

        //--------------
        //fill fields
        string postDtoTypeName = ctx.postTypeName + "_dto";
        query = baseQuery;
        ctx.ppt.Get.SetVariable("__ids", ids);
        AsType(postDtoTypeName);
        Where("__ids.Contains(post.Id)");
        ctx.ppt.Get.UnsetVariable("__ids");

        //return this;

        Type dtoType = ctx.mlocator.MetaMtoModelsCompiledTypeDict[postDtoTypeName];

        MethodInfo method = dtoType
              .GetMethods(BindingFlags.Instance | BindingFlags.Public)
              .First(mi => mi.Name == nameof(IDtoMarker.Fill)
                         // this check technically not required, but more future proof
                         && mi.GetParameters().Length == 1
                         && mi.GetParameters()[0].Name == "dataDict");

        var _list = query.ToList();

        foreach (var a in _list)
        {
            method.Invoke(a, new object[] { fillDict });
        }

        query = _list.AsQueryable();
        return this;
    }

    public TotalResponse<IBasicEntity> Table(string expr)
    {
        var args = TextHelper.SplitArguments(expr);
        MyThrowHelper.IfArgumentCount(args, 2, "arguments require 2");

        var z = ctx.ppt.GetParameters();

        int page = ctx.ppt.Get.Eval<int>(args[0], ctx.ppt.GetParameters());
        int size = ctx.ppt.Get.Eval<int>(args[1], ctx.ppt.GetParameters());

        var source = query;
        var filter = new QueryFilter(page, size);

        var items = source.Skip(filter.Skip).Take(filter.Take).ToList();
        int totalCount = source.Count();

        CurrentQueriGetTable = true;
        query = items.AsQueryable();


        return _tableResponse = new TotalResponse2<IBasicEntity>
        {
            Result = ETotalResponeResult.OK,
            Records = items,
            TotalCount = totalCount,
            Paginator = new PaginatorHelper(page, totalCount, size)
        };
    }

    public IDefaultEfQueries<IBasicEntity> Search(string searchText)
    {
        //var exp = ParseExp<IBasicEntity>(expr);
        //return query.Where(exp);

        //var result = CallQueryableMethod(nameof(Queryable.Where), );

        IQueryable<IBasicEntity> result;
        var _searchText = ctx.ppt.Get.Eval<string>(searchText).ToLower();

        if (typeof(Post).IsAssignableFrom(CurrentQueryEntityType))
        {
            result = (query as IQueryable<Post>)
                .Where(s =>
                s.Title.ToLower().Contains(_searchText)
                || s.Content.ToLower().Contains(_searchText));
        }
        else
        {
            result = query.Where(s => s.Id.ToString().Contains(_searchText));
        }


        query = result as IQueryable<IBasicEntity>;

        return this;
    }

    public IEnumerator<IBasicEntity> GetEnumerator()
    {
        return query?.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return (query as IEnumerable).GetEnumerator();
    }
}

public class TotalResponse2<T> : PagingResult<T>
{
    public TotalResponse2(IReadOnlyCollection<T> items, int page, int pageSize, bool hasMoreData, int? totalCount = null) : base(items, page, pageSize, hasMoreData, totalCount)
    {
    }

    public required PaginatorHelper Paginator { get; init; }
}

#else

using System.Collections;
using System.Linq.Expressions;
using Mars.Host.Data.Common;
using Mars.Host.QueryLang;

public class DefaultEfQueries : IDefaultEfQueries<IBasicEntity>
{
    public Type ElementType { get; } = default!;
    public Expression Expression { get; } = default!;
    public IQueryProvider Provider { get; } = default!;

    public int Count(string expr = "")
    {
        throw new NotImplementedException();
    }

    public IBasicEntity First(string expr = "")
    {
        throw new NotImplementedException();
    }

    public IEnumerator<IBasicEntity> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public IBasicEntity Last(string expr = "")
    {
        throw new NotImplementedException();
    }

    public IDefaultEfQueries<IBasicEntity> OrderBy(string fieldName)
    {
        throw new NotImplementedException();
    }

    public IDefaultEfQueries<IBasicEntity> OrderByDescending(string fieldName)
    {
        throw new NotImplementedException();
    }

    public IDefaultEfQueries<IBasicEntity> Skip(int count)
    {
        throw new NotImplementedException();
    }

    public IDefaultEfQueries<IBasicEntity> Take(int count)
    {
        throw new NotImplementedException();
    }

    public IDefaultEfQueries<IBasicEntity> Where(string expr)
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }

    object? IDynamicEfQuery.Last(string expr)
    {
        throw new NotImplementedException();
    }
}
#endif
