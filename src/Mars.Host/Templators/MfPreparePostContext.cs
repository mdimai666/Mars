using Mars.Host.Data;
using Mars.Host.Data.Contexts;
using Mars.Host.QueryLang;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Dto.Profile;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Templators;
//using Mars.Host.Templators.HandlebarsFunc;
using Mars.Shared.Contracts.MetaFields;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mars.Host.Templators;

public class MfPreparePostContext
{
    public MarsDbContext ef;
    public PostDetail post;
    public PostTypeDetail postType;
    public UserDetail user;
    public IReadOnlyCollection<MetaFieldDto> userMetaFields;


    public static JsonObject AsJson2(ref MfPreparePostContext pctx, IReadOnlyCollection<MetaValueDto> metaValues, IServiceProvider sp)
    {
        //IMetaModelTypesLocator mlocator = sp.GetRequiredService<IMetaModelTypesLocator>();

        //var tree = MetaValueTree.AsTree(metaValues);

        //JsonObject json = new();

        //Dictionary<Guid, MetaRelationObjectDict> dict = MyHandlebars.FillData(metaValues, sp);
        //JsonSerializerOptions opt = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        //{
        //    MaxDepth = 0,
        //    //IgnoreReadOnlyProperties = true,
        //    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        //};

        //foreach (var f in tree)
        //{
        //    AsJsonValue(ref json, f, ref pctx, ref dict, opt, mlocator);
        //}

        //return json;
        throw new NotImplementedException();
    }

    public static string AsJson2String(ref MfPreparePostContext pctx, ICollection<MetaValueDto> metaValues, /*ICollection<MetaField> metaFields,*/ IServiceProvider sp)
    {
        throw new NotImplementedException();
        //var json = AsJson2(ref pctx, metaValues, metaFields, sp);

        //JsonSerializerOptions opt = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        //{
        //    MaxDepth = 0,
        //    //IgnoreReadOnlyProperties = true,
        //    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,

        //};

        ////return json.ToJsonString(opt);
        //return json.ToJsonString(opt);
    }

    static void AsJsonValue(ref JsonObject json, MetaValueTree f, ref MfPreparePostContext q, ref Dictionary<Guid, MetaRelationObjectDict> dict, JsonSerializerOptions opt, IMetaModelTypesLocator mlocator, int depth = 0)
    {
        throw new NotImplementedException();

        //if (depth > 1000)
        //{
        //    return;
        //}

        //if (f.Type == MetaFieldType.List)
        //{
        //    var list = new JsonArray();

        //    foreach (var v in f.Childs)
        //    {
        //        var item = new JsonObject();
        //        if (v.Childs is null) continue;
        //        foreach (var a in v.Childs)
        //        {
        //            AsJsonValue(ref item, a, ref q, ref dict, opt, mlocator, depth + 1);
        //        }
        //        list.Add(item);
        //    }
        //    json.Add(f.Key, list);
        //}
        //else if (f.Type == MetaFieldType.Group)
        //{
        //    var group = new JsonObject();

        //    foreach (var v in f.Childs)
        //    {
        //        AsJsonValue(ref group, v, ref q, ref dict, opt, mlocator, depth + 1);
        //    }
        //    json.Add(f.Key, group);
        //}
        //else
        //{
        //    if (MetaValue.ERelations.Contains(f.Type))
        //    {

        //        if (f.Value is Guid _id)
        //        {
        //            //Console.WriteLine("333=" + _id);

        //            if (_id == Guid.Empty)
        //            {
        //                json.Add(f.Key, null);
        //            }
        //            else if (dict.ContainsKey(_id))
        //            {
        //                //var el = dict.Select(s => s.Value.Entity); for test

        //                MetaRelationObjectDict e = dict[_id];
        //                Type t = mlocator.GetModelType(e.Type, e.ModelName);
        //                dynamic originalTypeObject = Convert.ChangeType(e.Entity, t);
        //                //TODO: Проверить это, иожет излишне. Функция замены Id на саму модель
        //                if (t == typeof(User))
        //                {
        //                    UserDto userDto = new UserDto(originalTypeObject);
        //                    json.Add(f.Key, JsonValue.Create(userDto));
        //                }
        //                //else if (e is IBasicUserEntity)
        //                //{
        //                //    JsonValue j = JsonValue.Create(originalTypeObject);
        //                //    j.add there hide user sense data
        //                //}
        //                else if (e.Entity is not null)
        //                {
        //                    if (originalTypeObject is IBasicUserEntity basicUserEntity)
        //                    {
        //                        //System.Text.Json.json
        //                        //JsonValue j1 = JsonValue.Create(originalTypeObject);
        //                        //JsonObject j = new JsonObject();

        //                        //foreach(var a in j1.)
        //                        //{

        //                        //}
        //                        //Костыль. Надо было просто подменить свойство Юзер
        //                        if (basicUserEntity.User is not null)
        //                        {
        //                            var jj = JsonSerializer.Serialize(originalTypeObject, opt);
        //                            var j = JsonObject.Parse(jj);

        //                            //var j = JsonObject.(j1);
        //                            j.Remove(nameof(IBasicUserEntity.User).ToLower());
        //                            UserDto userDto = new(basicUserEntity.User);
        //                            j.Add(nameof(IBasicUserEntity.User).ToLower(), JsonValue.Create(userDto));

        //                            json.Add(f.Key, j);
        //                        }
        //                        else
        //                        {
        //                            json.Add(f.Key, JsonValue.Create(originalTypeObject));
        //                        }

        //                    }
        //                    else
        //                    {
        //                        json.Add(f.Key, JsonValue.Create(originalTypeObject));
        //                    }

        //                }
        //            }
        //            else
        //            {
        //                json.Add(f.Key, JsonValue.Create(f.Value));
        //            }
        //        }

        //    }
        //    else
        //    {
        //        json.Add(f.Key, JsonValue.Create(f.Value));
        //    }
        //}
    }
}
