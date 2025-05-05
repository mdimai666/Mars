using System;
using AppShared.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using AppShared.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Mars.Host.Data;
using Mars.Host.QueryLang;
using AppShared.Dto;
using Mars.Host.Shared.Services;

namespace Mars.Host.Services;


public class FormService : MinimalService
{

    public FormService(IConfiguration configuration, IServiceProvider serviceProvider) : base(configuration, serviceProvider)
    {

    }

    public async Task<IEnumerable<TitleEntity>> SelectVariants(string modelName, MarsDbContextLegacy _ef = null)
    {
        using var ef = _ef ?? GetEFContext();
        IMetaModelTypesLocator mlocator = _serviceProvider.GetRequiredService<IMetaModelTypesLocator>();
        Type model = mlocator.GetModelType(EMetaFieldType.Int, modelName);//TODO: int templary solution

        var user = GetUser();

        if (model == typeof(Post))
        {
            if (modelName == model.Name)
                return await ef.Posts.Select(s => new TitleEntity(s)).ToListAsync();
            else
                return await ef.Posts.Where(s => s.Type == modelName).Select(s => new TitleEntity(s)).ToListAsync();
        }
        else if (model == typeof(User))
        {
            return ef.Users.Select(s => new TitleEntity(s)).ToList();
        }
        //else if (model == typeof(PostCategory))
        //{
        //    return ef.PostCategories.Select(s => new TitleEntity(s)).ToList();
        //}
        //else if (model == typeof(NavMenu))
        //{
        //    return ef.NavMenus.Select(s => new TitleEntity(s)).ToList();
        //}
        //else if (model == typeof(Role))
        //{
        //    return ef.Roles.Select(s => new TitleEntity(s)).ToList();
        //}
        else if (model == typeof(FileEntity))
        {
            if (user is null) return default;
            return ef.Files.Where(s => s.UserId == user.Id).Select(s => new TitleEntity(s)).ToList();
        }

        var query = mlocator.GetModelQueryable(_serviceProvider, modelName);

        var list = query.ToList();

        return list.Select(s => new TitleEntity(s)).ToList();

    }

    public async Task<UserActionResult<Guid>> FormAdd(string modelName, JObject form, IFormFileCollection? files = null)
    {
        string _postType = modelName;

        if (string.IsNullOrEmpty(modelName)) throw new ArgumentNullException(nameof(modelName));

        var ps = _serviceProvider.GetRequiredService<PostTypeService>();

        PostType postType = await ps.Get(s => s.TypeName == _postType);
        if (postType is null) return new UserActionResult<Guid> { Message = $"Type \"{modelName}\" not found" };

        Post post = ParseJsonToPost(postType, form);
        post.Id = Guid.NewGuid();
        post.Type = modelName;

        PostService postService = _serviceProvider.GetRequiredService<PostService>();

        var user = GetUser();
        var ef = GetEFContext();

        if (user is null)
        {

            post.UserId = ef.Users.First(s => s.Email == "admin@mail.ru").Id;
        }

        if (files is not null)
        {
            FileService _fileService = _serviceProvider.GetRequiredService<FileService>();
            string file_group = "attachment";
            post.FileList ??= new();

            Guid userId;

            if (_userId is not null)
            {
                userId = _userId.Value;
            }
            else
            {
                userId = post.UserId;
            }

            foreach (var file in files)
            {


                UserActionResult<FileEntity> action = _fileService.WriteUpload<Post>(null, file, EFileType.PostAttachment, file_group, userId);
                if (action.Ok == false)
                {
                    return new UserActionResult<Guid>
                    {
                        Message = action.Message,
                    };
                }
                post.FileList.Add(action.Data);
            }
        }

        var addedPost = await postService.Add(post);
        //if (addedPost != null) не требуется
        //{
        //    //var _post = ef.Posts.Include(s => s.FileList).First(s => s.Id == post.Id);
        //    //_post.FileList = post.FileList;
        //    foreach (var file in post.FileList)
        //    {
        //        ef.PostFiles.Add(new PostFiles
        //        {
        //            FileEntityId = file.Id,
        //            PostId = addedPost.Id,
        //        });
        //    }
        //    ef.SaveChanges();
        //}

        if (addedPost is null) return new UserActionResult<Guid> { Message = "Не удалость записать" };

        return new UserActionResult<Guid> { Ok = true, Message = "Успешно добавлено", Data = addedPost.Id };
    }

    public async Task<UserActionResult> FormUpdate(string modelName, Guid id, JObject form)
    {
        string _postType = modelName;

        if (string.IsNullOrEmpty(modelName)) throw new ArgumentNullException(nameof(modelName));

        var ps = _serviceProvider.GetRequiredService<PostTypeService>();

        PostType postType = await ps.Get(s => s.TypeName == _postType);

        PostService postService = _serviceProvider.GetRequiredService<PostService>();

        Post existPost = await postService.Get(s => s.Id == id);

        //JObject existPostAsJson = JObject.FromObject(await postService.GetAsJson(s => s.Id == id));
        JObject existPostAsJson = JObject.FromObject(new PostDto(existPost));
        existPostAsJson.Remove(nameof(PostDto.User));
        existPostAsJson.Add(nameof(Post.UserId), existPost.UserId);

        var meOpt = new JsonMergeSettings
        {
            // union array values together to avoid duplicates
            MergeArrayHandling = MergeArrayHandling.Union,
        };

        existPostAsJson.Merge(form, meOpt);

        Post post = ParseJsonToPost(postType, existPostAsJson);
        post.Id = id;

        //foreach (var f in post.MetaValues)
        //{
        //    bool wasFound = false;
        //    foreach(var fe in existPost.MetaValues)
        //    {
        //        if(f.MetaField.Key == fe.MetaField.Key && f.MetaField.ParentId == fe.MetaField.ParentId)
        //        {
        //            fe.Set(f.MetaField, fe.Get());
        //            wasFound = true;
        //        } 
        //    }
        //    if (!wasFound)
        //    {
        //        existPost.MetaValues.Add(f);
        //    }
        //}

        foreach (var fe in existPost.MetaValues)
        {
            var exist = post.MetaValues.FirstOrDefault(f => f.MetaField.Key == fe.MetaField.Key && f.MetaField.ParentId == fe.MetaField.ParentId);
            if (exist is null)
            {
                post.MetaValues.Add(fe);
            }
        }

        var updatedPost = await postService.Update(id, post);

        return new UserActionResult { Ok = true, Message = "Успешно обновлено" };
    }


    public Post ParseJsonToPost(PostType postType, JObject form)
    {
        ICollection<MetaField> metaFields = postType.MetaFields;

        Post post = form.ToObject<Post>();
        if (post.Id == Guid.Empty)
        {
            post.Id = Guid.NewGuid();
        }

        if (string.IsNullOrEmpty(post.Title)) throw new ArgumentNullException("Title is required");

        var root = metaFields.Where(s => s.ParentId == Guid.Empty);

        List<MetaValue> list = new();

        string[] modelFields = { nameof(Post.Title), nameof(Post.Content),
                nameof(Post.Tags), nameof(Post.Status), nameof(Post.CategoryId),
                nameof(Post.Excerpt), nameof(Post.UserId) };

        foreach (var f in root)
        {
            var exist = form.ContainsKey(f.Key);
            if (exist)
            {
                if (f.TypeParentable == false)
                {

                    JToken _val = form[f.Key];
                    string val = "";
                    JArray arr = null;

                    if (_val is JArray)
                    {
                        arr = _val as JArray;
                    }
                    else if (_val is JObject)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        val = form.Value<string>(f.Key);//TODO: тут не строка
                    }


                    MetaValue metaValue = new MetaValue();
                    if(metaValue.Id != Guid.Empty)
                    {
                        metaValue.Id = Guid.NewGuid();
                    }
                    metaValue.MetaFieldId = f.Id;
                    metaValue.MetaField = f;
                    metaValue.Type = f.Type;

                    if (f.Type is EMetaFieldType.String or EMetaFieldType.Text)
                    {
                        metaValue.Set(f, val);
                    }
                    else if (f.Type == EMetaFieldType.Bool)
                    {
                        metaValue.Set(f, bool.Parse(val));
                    }
                    else if (f.Type == EMetaFieldType.Int)
                    {
                        metaValue.Set(f, int.Parse(val));
                    }
                    else if (f.Type == EMetaFieldType.Long)
                    {
                        metaValue.Set(f, long.Parse(val));
                    }
                    else if (f.Type == EMetaFieldType.Float)
                    {
                        metaValue.Set(f, float.Parse(val));
                    }
                    else if (f.Type == EMetaFieldType.Decimal)
                    {
                        metaValue.Set(f, decimal.Parse(val));
                    }

                    else if (f.Type == EMetaFieldType.DateTime)
                    {
                        metaValue.Set(f, DateTime.Parse(val));
                    }

                    else if (f.Type == EMetaFieldType.Select)
                    {
                        if (Guid.TryParse(val, out Guid varinatId))
                        {
                            metaValue.Set(f, varinatId);
                        }
                        else
                        {
                            MetaFieldVariant found = f.Variants.Find(s => s.Title == val);
                            if (found is null) throw new KeyNotFoundException($"for key \"{f.Key}\" variant \"{val}\" not found");
                            metaValue.Set(f, found.Id);
                        }
                    }
                    else if (f.Type == EMetaFieldType.SelectMany)
                    {
                        //Guid[] varinatId = val.Split(',').Select(Guid.Parse).ToArray();
                        //metaValue.Set(f, varinatId);

                        List<Guid> variants = new();

                        foreach (var _v in arr)
                        {
                            var v = _v.ToString();
                            if (Guid.TryParse(v, out Guid varinatId))
                            {
                                variants.Add(varinatId);
                            }
                            else
                            {
                                MetaFieldVariant found = f.Variants.Find(s => s.Title == v);
                                if (found is null) throw new KeyNotFoundException($"for key \"{f.Key}\" variant \"{val}\" not found");
                                variants.Add(found.Id);
                            }
                        }

                        metaValue.Set(f, variants);
                    }
                    else if (f.TypeRelation)
                    {
                        Guid modelId = Guid.Parse(val);

                        metaValue.Set(f, modelId);

                    }
                    else
                    {
                        throw new NotImplementedException($"for type {f.Type}");
                    }

                    list.Add(metaValue);

                }
            }
        }


        post.Type = postType.TypeName;
        //post.Slug = Tools.TranslateToPostSlug(post.Title);
        if (string.IsNullOrEmpty(post.Slug))
        {
            post.Slug = post.Id.ToString();
        }
        post.MetaValues = list;
        post.LikesCount = 0;
        post.CommentsCount = 0;

        return post;

    }

    //void CopyNonEmptyFields(Post post, Post copy)
    //{
    //    if (!string.IsNullOrEmpty(copy.Slug)) post.Slug = copy.Slug;
    //    if (!string.IsNullOrEmpty(copy.Title)) post.Title = copy.Title;
    //    if (!string.IsNullOrEmpty(copy.Excerpt)) post.Excerpt = copy.Excerpt;
    //    if (!string.IsNullOrEmpty(copy.Image)) post.Image = copy.Image;
    //    if (!string.IsNullOrEmpty(copy.Status)) post.Status = copy.Status;
    //    if (!string.IsNullOrEmpty(copy.Lang)) post.Lang = copy.Lang;

    //    var dict = post.MetaValues.Where(s => s.ParentId == Guid.Empty)
    //        .ToDictionary(s => s.MetaField.Key);

    //    foreach (var f in copy.MetaValues)
    //    {
    //        string key = f.MetaField.Key;

    //        //if (dict.TryGetValue(key, out var val))
    //        //{

    //        //}

    //        if (dict.ContainsKey(key))
    //        {
    //            dict[key] = f;
    //        }
    //        else
    //        {
    //            post.MetaValues.Add(f);
    //        }

    //    }

    //}
}

