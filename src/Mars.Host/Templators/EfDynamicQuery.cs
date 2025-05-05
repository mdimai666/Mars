using System.Linq.Expressions;
using Mars.Host.Data;
using Mars.Host.QueryLang;
using Mars.Host.Services;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Templators;
using Mars.Core.Features;
using DynamicExpresso;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Mars.Host.Shared.Dto.Profile;
using Mars.Host.Data.Contexts;
using Mars.Shared.Contracts.MetaFields;
using Mars.Host.Data.Entities;
using Mars.Host.Data.Common;
using Mars.Host.Shared.Dto.PostTypes;

namespace Mars.Host.Templators;


#if false
public class EfDynamicQuery<TEntity> where TEntity : class, IBasicEntity, new()
{

    public async Task Query(string key, string val, int index, TemplatorContextPreparatorContext pctx, XInterpreter ppt)
    {
        Dictionary<string, object> context = pctx.context;

        Action<string> addErr = pctx.addErr;
        MarsDbContext ef = pctx.ef;
        var postService = pctx.serviceProvider.GetRequiredService<IPostJsonService>();
        //Post post = pctx.post;
        //PostType postType = pctx.postType;
        var user = pctx.user;
        //WebClientRequest req = pctx.req;
        //QServer http = pctx.GetService<QServer>();
        var userService = pctx.serviceProvider.GetRequiredService<IUserService>();
        IMetaModelTypesLocator mlocator = pctx.serviceProvider.GetRequiredService<IMetaModelTypesLocator>();
        //MetaFieldService metaFieldService = pctx.serviceProvider.GetRequiredService<MetaFieldService>();

        var spl = val.Split('.', count: 2);
        string cmd = spl[0];

        var chain = TextHelper.ParseChainPairKeyValue(val);

        var postTypes = ef.PostTypes.Include(s => s.MetaFields).ToList();
        var postTypesDict = postTypes.ToDictionary(s => s.TypeName);
        var postTypeNames = postTypes.Select(s => s.TypeName).ToList();
        WebSiteTypesInfo info = new(postTypeNames);

        //ef.Posts.ToList().TakeRandom
        //ef.Posts.ToList().TakeRandomRange


        //var query = ef.Posts.AsQueryable();
        Type entityType = mlocator.GetModelType(MetaFieldType.Bool, cmd);

        bool isPost = entityType == typeof(PostEntity);
        string postTypeName = cmd;
        //string expVarName = "_" + cmd.ToLower();
        string expVarName = "post";

        IQueryable<TEntity> query = MarsDbContextHelper.DbSetByType(entityType, ef, pctx.serviceProvider) as dynamic;

        if (isPost && postTypeName != nameof(PostEntity) && !postTypeNames.Contains(postTypeName))
        {
            throw new ArgumentException($"postType \"{postTypeName}\" not found");
        }

        if (isPost && postTypeName != nameof(PostEntity))
        {
            query = query.Where(s => (s as PostEntity).PostType.TypeName == postTypeName);
        }
        query = query.OrderByDescending(s => s.CreatedAt);

        bool isOneRecordRequest = info.IsOneQuery(val);


        //is one query
        if (isOneRecordRequest)
        {
            var a = chain.First();
            //костыль
            //////////{
            //////////    int ind = val.IndexOf(a.Key);
            //////////    string _v = val.Substring(ind + a.Key.Length + 1);
            //////////    _v = _v.Substring(0, _v.Length - 1);
            //////////    a = new KeyValuePair<string, string>(a.Key, _v);
            //////////}

            bool noArg = string.IsNullOrEmpty(a.Value);

            if (a.Key == "Count")
            {
                if (context.ContainsKey(key)) context.Remove(key);

                int _count = 0;
                if (noArg)
                {
                    _count = query.Count();
                }
                else
                {

                    var expression = parseExpression(ppt, entityType, a.Value, expVarName);
                    _count = query.Count(expression);
                }
                context.Add(key, _count);
                ppt.parameters.Add(key, new Parameter(key, _count));
                return;
            }

            TEntity post1;

            if (a.Key == "First")
            {
                if (noArg)
                {
                    post1 = query.FirstOrDefault();
                }
                else
                {
                    var expression = parseExpression(ppt, entityType, a.Value, expVarName/*, pt*/);
                    post1 = query.FirstOrDefault(expression);
                }
            }
            else if (a.Key == "Last")
            {
                if (noArg)
                {
                    post1 = query.LastOrDefault();
                }
                else
                {
                    var expression = parseExpression(ppt, entityType, a.Value, expVarName);
                    post1 = query.LastOrDefault(expression);

                }
            }
            //else if (a.Key == "Meta")
            //{
            //    var pt = postTypes.First(s => s.TypeName == postTypeName);

            //    //MetaField mf = ef.Posts

            //    object[] args = a.Value.Split(",");

            //    string salaryKey = "int1";


            //    post1 = ((IQueryable<Post>)query)
            //        .Include(s => s.MetaValues)
            //        .ThenInclude(s => s.MetaField)
            //        .FirstOrDefault(s => s.MetaValues.Any(x => x.MetaField.Key == salaryKey && x.Int == 11)) as TEntity;
            //}
            else
            {
                addErr($"$context => qq [key]={key} command: {a.Key} not support (index:{index})");
                return;
            }

            if (context.ContainsKey(key)) context.Remove(key);

            if (post1 == null)
            {
                context.Add(key, null);
            }
            else
            {
                if (entityType == typeof(PostEntity))
                {
                    //var post1_wm = await postService.Get(ef, s => s.Id == post1.Id);
                    Post post1_wm;
                    throw new NotImplementedException("XxxxxxxxxxxXxxxxxxxxxx");
                    var post1_type = ef.PostTypes.Include(s => s.MetaFields)
                        .FirstOrDefault(s => s.TypeName == post1_wm.Type);

                    MfPreparePostContext pctx2 = new MfPreparePostContext
                    {
                        ef = ef,
                        post = post1_wm,
                        postType = post1_type,
                        user = user,
                        userMetaFields = pctx.userMetaFields,
                    };

                    var _postJson = postService.AsJson22(pctx2);
                    //var _context = JObject.Parse(_postJson);
                    var _context = _postJson;
                    context.Add(key, _context);
                    ppt.parameters.Add(key, new Parameter(key, _context));
                }
                else if (entityType == typeof(User))
                {
                    var _user = await (query as IQueryable<User>)
                    .Include(s => s.MetaValues)
                            .ThenInclude(s => s.MetaField)
                    .FirstAsync(s => s.Id == post1.Id);

                    _user.MetaValues = metaFieldService.GetValuesBlank(_user.MetaValues, pctx.userMetaFields);

                    MfPreparePostContext pctx3 = new MfPreparePostContext
                    {
                        ef = ef,
                        post = null,
                        postType = null,
                        user = user,
                        userMetaFields = pctx.userMetaFields,
                    };

                    var _userJson = userService.AsJson22(pctx3, _user);

                    if (context.ContainsKey(key)) context.Remove(key);

                    context.Add(key, JToken.FromObject(_userJson));
                    ppt.parameters.Add(key, new Parameter(key, _userJson));

                }
                else //if(entityType == typeof(PostType))
                {


                    //var _postJson = await postService.AsJson2(po);
                    var _context = JObject.FromObject(post1);

                    //var _context = (_postJson);
                    context.Add(key, _context);
                    ppt.parameters.Add(key, new Parameter(key, _context));
                }
                //else
                //{
                //    throw new NotImplementedException($"Not Implemented for type {postTypeName}");
                //}
            }
        }
        else if (chain.Count() > 0)
        {
            bool breaked = false;

            //vacancy.first
            //post.

            foreach (var a in chain)
            {
                if (a.Key == "Where")
                {
                    var expression = parseExpression(ppt, entityType, a.Value, expVarName);
                    query = query.Where(expression);
                }
                else if (a.Key == "Skip")
                {
                    var _result = ppt.Get.Eval<int>(a.Value, ppt.GetParameters());
                    query = query.Skip(_result);
                }
                else if (a.Key == "Take")
                {
                    var _result = ppt.Get.Eval<int>(a.Value, ppt.GetParameters());
                    query = query.Take(_result);
                }
                //else if (a.Key == "Meta")
                //{
                //    var pt = postTypes.First(s => s.TypeName == postTypeName);

                //    //MetaField mf = ef.Posts

                //    object[] args = a.Value.Split(",");

                //    string salaryKey = "int1";


                //    query = ((IQueryable<Post>)query)
                //        .Include(s => s.MetaValues)
                //        .ThenInclude(s => s.MetaField);
                //        //.Where(s => s.MetaValues.Any(x => x.MetaField.Key == salaryKey && x.Int == 11));
                //}
                else
                {
                    addErr($"$context => chains [key]={key} command: {a.Key} not support (index:{index})");
                    breaked = true;
                    break;
                }
            }
            if (breaked) return;


            //var posts = await ef.Posts.ToListAsync();
            //var result = await ef.Posts.Where(expression).ToListAsync();
            if (entityType == typeof(PostEntity))
            {
                var result = await (query as IQueryable<Post>)
                        .Include(s => s.FileList)
                        //.ThenInclude(s => s.FileEntity)
                        .Include(s => s.User)
                        .Include(s => s.MetaValues)
                            .ThenInclude(s => s.MetaField)
                    .ToListAsync();

                if (context.ContainsKey(key)) context.Remove(key);

                if (result.Count == 0)
                {
                    context.Add(key, new string[] { });

                }
                else
                {

                    foreach (var p in result)
                    {
                        string _postTypeName = p.Type;
                        if (string.IsNullOrEmpty(_postTypeName) == false)
                        {
                            var _postType = postTypesDict[_postTypeName];
                            p.MetaValues = metaFieldService.GetValuesBlank(p.MetaValues, _postType.MetaFields);
                        }
                    }

                    //List<string> jj = new();
                    JArray arr = new JArray();

                    foreach (var p in result)
                    {
                        //string _postJson = await postService.AsJson(p, _postType);
                        //jj.Add(_postJson);
                        string _postTypeName = p.Type;
                        if (string.IsNullOrEmpty(_postTypeName)) continue;

                        var _postType = postTypesDict[p.Type];

                        MfPreparePostContext pctx3 = new MfPreparePostContext
                        {
                            ef = ef,
                            post = p,
                            postType = _postType,
                            user = user,
                            userMetaFields = pctx.userMetaFields,

                        };

                        //string _postJson = await postService.AsJson2(pctx3);
                        var _postJson = postService.AsJson22(pctx3);
                        arr.Add(_postJson);


                    }

                    var _context = arr;
                    context.Add(key, _context);
                    ppt.parameters.Add(key, new Parameter(key, _context));
                }
            }
            else if (entityType == typeof(User))
            {
                var result = await (query as IQueryable<User>)
                    .Include(s => s.MetaValues)
                            .ThenInclude(s => s.MetaField)
                    .ToListAsync();


                if (context.ContainsKey(key)) context.Remove(key);

                if (result.Count == 0)
                {
                    context.Add(key, new string[] { });

                }
                else
                {

                    foreach (var p in result)
                    {
                        p.MetaValues = metaFieldService.GetValuesBlank(p.MetaValues, pctx.userMetaFields);
                    }

                    JArray arr = new JArray();

                    foreach (var p in result)
                    {
                        MfPreparePostContext pctx3 = new MfPreparePostContext
                        {
                            ef = ef,
                            post = null,
                            postType = null,
                            user = user,
                            userMetaFields = pctx.userMetaFields,
                        };

                        //string _postJson = await postService.AsJson2(pctx3);
                        var _userJson = userService.AsJson22(pctx3, p);
                        arr.Add(_userJson);

                    }

                    var _context = arr;
                    context.Add(key, _context);
                    ppt.parameters.Add(key, new Parameter(key, _context));
                }
            }
            else
            {
                //throw new NotImplementedException($"Not Implemented for type {postTypeName}");

                var result = await query.ToListAsync();


                if (context.ContainsKey(key)) context.Remove(key);

                if (result.Count == 0)
                {
                    context.Add(key, new string[] { });

                }
                else
                {
                    //JArray arr = new JArray();

                    //foreach (var p in result)
                    //{

                    //    //string _postJson = await postService.AsJson2(pctx3);

                    //    var _context = JObject.FromObject(post1);

                    //    //var _context = (_postJson);
                    //    context.Add(key, _context);
                    //    ppt.parameters.Add(key, new Parameter(key, _context));
                    //}

                    var _context = JArray.FromObject(result);
                    context.Add(key, _context);
                    ppt.parameters.Add(key, new Parameter(key, _context));
                }
            }



        }
    }

    public Expression<Func<TEntity, bool>> parseExpression(XInterpreter ppt, Type entityType, string text, string varible, PostType pt = null)
    {
        if (false)
        {
            //Expression<Func<IBasicEntity, bool>> expression = ppt.Get.ParseAsExpression<Func<IBasicEntity, bool>>(text, varible);
            //return expression;
        }

        //Expression<Func<T, bool>> x = null;

        //switch (entityType.Name)
        //{
        //    case nameof(PostEntity): x = ppt.Get.ParseAsExpression<Func<Post, bool>>(text, varible); break;
        //    case nameof(User): x = ppt.Get.ParseAsExpression<Func<User, bool>>(text, varible); break;
        //    case nameof(NavMenu): x = ppt.Get.ParseAsExpression<Func<NavMenu, bool>>(text, varible); break;
        //    case nameof(FileEntity): x = ppt.Get.ParseAsExpression<Func<FileEntity, bool>>(text, varible); break;
        //    case nameof(PostCategory): x = ppt.Get.ParseAsExpression<Func<PostCategory, bool>>(text, varible); break;
        //    case nameof(PostType): x = ppt.Get.ParseAsExpression<Func<PostType, bool>>(text, varible); break;
        //    case nameof(Role): x = ppt.Get.ParseAsExpression<Func<Role, bool>>(text, varible); break;
        //    case nameof(Option): x = ppt.Get.ParseAsExpression<Func<Option, bool>>(text, varible); break;
        //    default: throw new NotImplementedException();
        //}

        //Expression<Func<IBasicEntity, bool>> result = (Expression<Func<IBasicEntity, bool>>)Convert.ChangeType(x, typeof(Expression<Func<IBasicEntity, bool>>));

        //var result = ppt.Get.ParseAsExpression<Func<object, bool>>(text, varible);

        //return result;

        //Regex fieldsReg = new Regex(@"post\.(\w+)");

        //var matches = fieldsReg.Matches(text);
        //var flist = matches.Select(s => s.Groups[1]);

        //var props = typeof(TEntity).GetProperties();

        //if (pt is not null)
        //{
        //    var fn = flist.First().Value;
        //    var mf = pt.MetaFields.FirstOrDefault(s => s.Key == fn);

        //    string q = $"post.MetaField"

        //}


        return ppt.Get.ParseAsExpression<Func<TEntity, bool>>(text, varible);
    }
}

//public class DimExpresso<T> where T : IBasicEntity
//{
//    IQueryable<T> query;
//    XInterpreter ppt;
//    string varName;

//    public DimExpresso(IQueryable<T> query, XInterpreter ppt, string varName)
//    {
//        this.query = query;
//        this.ppt = ppt;
//        this.varName = varName;
//    }


//    public T First()
//    {
//        return query.First();
//    }

//    public T First(string args)
//    {
//        var exp = ppt.Get.ParseAsExpression<Func<T, bool>>(args, varName);
//        return query.First(exp);
//    }
//}

#endif

public class EfDynamicQuery<TEntity> where TEntity : class, IBasicEntity, new()
{
    public async Task Query(string key, string val, int index, object pctx, XInterpreter ppt)
    {
        throw new NotImplementedException();
    }

    public Expression<Func<TEntity, bool>> parseExpression(XInterpreter ppt, Type entityType, string text, string varible, PostTypeDetail pt = null)
    {
        throw new NotImplementedException();
    }
}
