using System;
using Mars.Host.Data;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Templators;
using Mars.Core.Features;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Mars.Host.Data.Entities;
using Mars.Shared.Contracts.MetaFields;
using Mars.Host.Data.Contexts;
using Mars.Host.Shared.Dto.Users;

namespace Mars.Host.QueryLang;

public class EntityQuery
{
    protected readonly IServiceProvider sp;
    protected readonly XInterpreter ppt;
    protected DefaultEfQueries qef;
    protected UserDetail? user;

    public EntityQuery(IServiceProvider sp, XInterpreter ppt, UserDetail? user)
    {
        this.sp = sp;
        this.ppt = ppt;
        this.user = user;
    }



    public object Query(string queryChain)
    {
        throw new NotImplementedException();
        //IMetaModelTypesLocator mlocator = sp.GetRequiredService<IMetaModelTypesLocator>();

        //string typeName = queryChain.Split('.')[0];
        //Type entityType = mlocator.GetModelType((MetaFieldType)EMetaFieldType.Int, typeName);

        //var query = mlocator.GetModelQueryable(sp, typeName);

        ////query = query.OrderByDescending(s => s.Created);// exception

        //bool triggerType = false;
        //if (entityType == typeof(PostEntity) && typeName != nameof(PostEntity))
        //{
        //    query = (query as IQueryable<PostEntity>).Where(s => s.PostType.TypeName == typeName);
        //    triggerType = true;

        //    mlocator.TryUpdateMetaModelMtoRuntimeCompiledTypes(sp);

        //    //query = (query as IQueryable<Post>).AsType(s => s.Type == typeName);
        //}

        ////bool isBasicEntity = entityType.IsAssignableTo(typeof(IBasicEntity));

        ////if (isBasicEntity) exception ordered querable
        ////{
        ////    query = query.OrderByDescending(s => s.Created);
        ////}


        ////remove
        //MarsDbContext ef = sp.GetService<MarsDbContext>();
        //var postTypes = ef.PostTypes.ToList();

        //qef = new DefaultEfQueries();
        //qef.query = query;
        //qef.baseQuery = query;
        //qef.ctx = new EfDynQueryDict.DQC_Context(sp, typeName, entityType, ef, ppt, new JObject(), postTypes, user);
        ////{
        ////    context = new JObject(),
        ////    ef = ef,
        ////    entityType = entityType,
        ////    //postTypes = postTypes,
        ////    ppt = ppt,
        ////    mlocator = mlocator,
        ////    postTypeName = typeName,
        ////    serviceProvider = sp,
        ////    user = user
        ////};
        ////end remove

        //var chains = TextHelper.ParseChainPair(queryChain);

        //if (triggerType)
        //{
        //    chains = chains.Prepend(new ChainPair(0, 0, 0, "AsType", typeName));
        //}

        //var map = qef.MethodsMapping();

        //object? result = null;

        //foreach (var func in chains)
        //{
        //    string name = func.Method;
        //    string args = func.Argument;

        //    if (map.ContainsKey(name) == false) throw new ArgumentNullException($"QEF Method \"{name}\" not found");

        //    var method = map[name];

        //    result = method.Invoke(qef, new[] { args });
        //}

        ////if (triggerType)
        ////{
        ////    if (result is not IEnumerable)
        ////    {
        ////        result = new ExpandoObject()
        ////    }
        ////}


        //return result;
    }
}
