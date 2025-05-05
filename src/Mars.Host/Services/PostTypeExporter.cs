using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Shared.Common;
using Mars.Shared.Contracts.MetaFields;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Services;

public class PostTypeExporter
{
    private readonly IServiceProvider serviceProvider;

    public PostTypeExporter(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public async Task<PostTypeExport> ExportPostType(Guid id)
    {
        PostTypeService postTypeService = serviceProvider.GetRequiredService<PostTypeService>();

        PostTypeDetail postType = default;// await postTypeService.Get1(id);

        PostTypeExport export = new PostTypeExport
        {
            Title = postType.Title,
            TypeName = postType.TypeName,
            EnabledFeatures = postType.EnabledFeatures,
            //PostContentType = postType.PostContentType,
            //PostStatusList = postType.PostStatusList.Select(s => new PostStatusExport(s)).ToList(),
            //MetaFields = MetaFieldExportFormat.GetExportList(postType.MetaFields.ToList())
        };

        return export;
    }

    public async Task<UserActionResult> ImportPostType(PostTypeExport import, string asPostType = "")
    {
        throw new NotImplementedException();
        //try
        //{
        //    if (string.IsNullOrWhiteSpace(import.TypeName)) throw new ArgumentNullException(nameof(import.TypeName));
        //    if (string.IsNullOrWhiteSpace(import.Title)) throw new ArgumentNullException(nameof(import.Title));

        //    string postTypeName = string.IsNullOrEmpty(asPostType) == false ? asPostType : import.TypeName;

        //    PostTypeService postTypeService = serviceProvider.GetRequiredService<PostTypeService>();

        //    //PostType postType = await postTypeService.Get(s => s.TypeName == postTypeName);

        //    var ef = serviceProvider.GetRequiredService<Data.MarsDbContextLegacy>();
        //    PostType postType = ef.PostTypes
        //            .Include(s => s.MetaFields)
        //            .AsNoTracking()
        //            .FirstOrDefault(s => s.TypeName == postTypeName);

        //    postType ??= new();
        //    if (postType.Id == Guid.Empty) postType.Id = Guid.NewGuid();

        //    postType.Title = import.Title;
        //    postType.TypeName = postTypeName;
        //    postType.EnabledFeatures = import.EnabledFeatures;
        //    postType.PostContentType = import.PostContentType;

        //    postType.PostStatusList = PostStatusExport.GetPostStatusFromImport(import.PostStatusList, postType.PostStatusList);
        //    postType.MetaFields = MetaFieldExportFormat.ImportFields(import.MetaFields, postType.MetaFields.ToList());

        //    //postType.Created

        //    await postTypeService.Update(postType.Id, postType);

        //    ////////var ef = serviceProvider.GetRequiredService<Data.MarsDbContext>();

        //    ////////ef.Entry(postType);//.CurrentValues.SetValues(entity);
        //    ////////postType.Modified = DateTime.Now;


        //    ////////var entity = postType;

        //    ////////await postTypeService.UpdateManyToMany(ef.PostTypeMetaFields, s => s.PostTypeId == entity.Id,
        //    ////////        s => s.PostTypeId, s => s.MetaFieldId,
        //    ////////        entity.Id, entity.MetaFields.Select(s => s.Id));

        //    ////////int state = await ef.SaveChangesAsync();

        //    return new UserActionResult
        //    {
        //        Ok = true,
        //        Message = "Успешно сохранено"
        //    };

        //}
        //catch (Exception ex)
        //{
        //    return new UserActionResult
        //    {
        //        Message = ex.Message
        //    };
        //}
    }
}

public class PostTypeExport
{
    public string Title { get; set; }
    public string TypeName { get; set; }
    public List<PostStatusExport> PostStatusList { get; set; }
    public IReadOnlyCollection<string> EnabledFeatures { get; set; } 
    [Display(Name = "Тип PostContent")]
    //public EPostContentType PostContentType { get; set; } = EPostContentType.PlainText;

    public List<MetaFieldExportFormat> MetaFields { get; set; }

}

public class PostStatusExport
{
    [Display(Name = "Название")]
    public string Title { get; set; }

    [Display(Name = "Значение")]
    public string Slug { get; set; }
    public IReadOnlyCollection<string> Tags { get; set; }

    public DateTime Created { get; set; }

    public PostStatusExport()
    {

    }

    public PostStatusExport(PostStatusDto postStatus)
    {
        Title = postStatus.Title;
        Slug = postStatus.Slug;
        //Created = postStatus.Created;
    }

    public static List<PostStatusDto> GetPostStatusFromImport(List<PostStatusExport> imports, List<PostStatusDto> exist)
    {
        var dict = exist.ToDictionary(s => s.Slug);

        return imports.Select(s => new PostStatusDto
        {
            //Created = s.Created,
            Title = s.Title,
            Slug = s.Slug,
            //Modified = DateTime.Now,
            Id = dict.ContainsKey(s.Slug) ? dict[s.Slug].Id : Guid.NewGuid(),
            Tags = s.Tags,
        }).ToList();
    }
}

public class MetaFieldExportFormat
{
    [Display(Name = "Название")]
    public string Title { get; set; }

    [Display(Name = "Key")]
    public string KeyPath { get; set; }


    [Display(Name = "Тип")]
    public MetaFieldType Type { get; set; }

    [Column(TypeName = "jsonb")]
    [Display(Name = "Варианты")]
    public IReadOnlyCollection<MetaFieldVariantDto> Variants { get; set; } 

    [Display(Name = "Максимальное")]
    public decimal? MaxValue { get; set; } = null;
    [Display(Name = "Минимальное")]
    public decimal? MinValue { get; set; } = null;

    [Display(Name = "Описание")]
    public string Description { get; set; } = "";

    [Display(Name = "IsNullable")]
    public bool IsNullable { get; set; }

    //[NotMapped]
    //[Column(TypeName = "jsonb")]
    //[Display(Name = "Значение по умолчанию")]
    //public MetaValue Default { get; set; }

    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }

    [Display(Name = "Порядок")]
    public int Order { get; set; }


    /////////////////////////////////////
    /// <summary>
    /// <seealso cref="MetaField.GetModelType(EMetaFieldType,string)"/>
    /// </summary>
    [Display(Name = "Модель")]
    public string ModelName { get; set; }

    public static List<MetaFieldExportFormat> GetExportList(List<MetaFieldDto> metaFields)
    {
        var dict = metaFields.ToDictionary(s => s.Id);

        //var dictKey = metaFields.ToDictionary(s => s.Key);
        var list = MetaFieldWithKeypath.GetWithKeypath(metaFields);

        return list.Select(s => new MetaFieldExportFormat
        {
            Title = s.Title,
            //Key = s.Key,
            //ParentKey = s.ParentId == Guid.Empty ? "" : dict[s.ParentId].Key,
            //KeyPath = GetKeyPath(s.Key, ref dictKey),
            KeyPath = s.Keypath,
            Description = s.Description,
            IsNullable = s.IsNullable,
            MinValue = s.MinValue,
            MaxValue = s.MaxValue,
            Order = s.Order,
            Type = s.Type,
            Variants = s.Variants,
            //Created = s.Created,
            //Modified = s.Modified,
            ModelName = s.ModelName

        }).ToList();
    }

    public static List<MetaFieldDto> ImportFields(List<MetaFieldExportFormat> import, List<MetaFieldDto> existMetaFields)
    {
        //var dictKey = exist.ToDictionary(s => s.Key);

        //List<MetaFieldExportKeyed> list = new();

        //List<MetaFieldExport> root = import.Where(s => s.KeyPath.Contains("/") == false).ToList();



        //foreach (var a in root)
        //{
        //    MetaFieldExportKeyed d = new MetaFieldExportKeyed(a);

        //    MetaField x = dictKey.ContainsKey(a.KeyPath) ? dictKey[a.KeyPath] : null;

        //    if (x != null) d.Id = x.Id;

        //    var childs = ScanChilds(a);

        //    if (childs != null) list.AddRange(childs);

        //}

        //return list;

        List<MetaFieldDto> list = new();

        List<MetaFieldWithKeypath> existWithKeypath = MetaFieldWithKeypath.GetWithKeypath(existMetaFields);
        var dictExist = existWithKeypath.ToDictionary(s => s.Keypath);

        foreach (var a in import)
        {
            MetaFieldWithKeypath exist = null;
            if (dictExist.ContainsKey(a.KeyPath)) exist = dictExist[a.KeyPath];

            MetaFieldDto meta;
            if (exist != null)
            {
                Guid parentId = Guid.Empty;
                if (a.KeyPath.Contains('/'))
                {
                    string parentKey = string.Join('/', a.KeyPath.Split('/').SkipLast(1));
                    parentId = dictExist[parentKey].Id;
                }
                meta = FromMetaFieldExportKeyed(a, exist.Id, parentId);
            }
            else
            {
                Guid parentId = Guid.Empty;
                if (a.KeyPath.Contains('/'))
                {
                    string parentKey = string.Join('/', a.KeyPath.Split('/').SkipLast(1));
                    if (dictExist.ContainsKey(parentKey))
                    {
                        parentId = dictExist[parentKey].Id;
                    }
                }
                meta = FromMetaFieldExportKeyed(a, Guid.NewGuid(), parentId);
            };

            list.Add(meta);

        }

        return list;
    }

    public static string GetKeyPath(Guid id, ref Dictionary<Guid, MetaFieldDto> dict)
    {
        var meta = dict[id];

        if (meta.ParentId == Guid.Empty)
        {
            return meta.Key;
        }
        else
        {
            string _key = GetKeyPath(meta.ParentId, ref dict);

            return _key + '/' + meta.Key;
        }
    }

    //public static string GetKeyPath(string key, ref Dictionary<string, MetaField> dict)
    //{
    //    var meta = dict[key];

    //    if (meta.ParentId == Guid.Empty)
    //    {
    //        return key;
    //    }
    //    else
    //    {
    //        string _key = GetKeyPath(key, ref dict);

    //        return _key + '/' + key;
    //    }
    //}

    static MetaFieldDto FromMetaFieldExportKeyed(MetaFieldExportFormat a, Guid existId, Guid parentId)
    {
        return new MetaFieldDto
        {
            Id = existId,
            Variants = a.Variants,
            //Created = a.Created,
            //Modified = a.Modified,
            //Default = a.Default,
            Description = a.Description,
            IsNullable = a.IsNullable,
            Key = a.KeyPath.Split('/').Last(),
            MaxValue = a.MaxValue,
            MinValue = a.MinValue,
            Order = a.Order,
            Type = a.Type,
            Title = a.Title,
            ParentId = parentId,
            ModelName = a.ModelName,

            Disabled = false,
            Hidden = false,
            Tags = []
        };
    }
}

internal class MetaFieldExportKeyed : MetaFieldExportFormat
{
    public Guid? Id { get; set; } = null;
    public Guid ParentId { get; set; }

    public MetaFieldExportKeyed(MetaFieldExportFormat a)
    {
        Type = a.Type;
        KeyPath = a.KeyPath;
        Variants = a.Variants;
        MinValue = a.MinValue;
        MaxValue = a.MaxValue;
        Description = a.Description;
        IsNullable = a.IsNullable;
        Order = a.Order;
        ModelName = a.ModelName;
    }
}

internal record MetaFieldWithKeypath : MetaFieldDto
{
    public string Keypath { get; set; } = "";

    public MetaFieldWithKeypath(MetaFieldDto f, string keypath)
    {
        Id = f.Id;
        Key = f.Key;
        Title = f.Title;
        Description = f.Description;
        //Created = f.Created;
        //Modified = f.Modified;
        IsNullable = f.IsNullable;
        MinValue = f.MinValue;
        MaxValue = f.MaxValue;
        //Default = f.Default;
        Order = f.Order;
        Type = f.Type;
        Variants = f.Variants;
        ModelName = f.ModelName;

        Keypath = keypath;
    }

    public static List<MetaFieldWithKeypath> GetWithKeypath(List<MetaFieldDto> metaFields)
    {
        //var dict = metaFields.ToDictionary(s => s.Id);
        ////var dictKey = metaFields.ToDictionary(s => MetaFieldExportFormat.GetKeyPath(s.Id, ref dict));

        //return metaFields.Select(s =>
        //{
        //    //string keypath = MetaFieldExportFormat.GetKeyPath(s.Key, ref dictKey);
        //    string keypath = MetaFieldExportFormat.GetKeyPath(s.Id, ref dict);
        //    return new MetaFieldWithKeypath(s, keypath) {  };
        //}).ToList();
        throw new NotImplementedException();
    }
}
