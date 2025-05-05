using System.Reflection;
using System.Text;
using AppShared.Dto;
using AppShared.Models;
using Mars.Host.Shared.Services;
using Mars.Core.Extensions;
using Mars.Host.Shared.Interfaces;

namespace Mars.GenSourceCode;

/// <summary>
/// this class like mto but have Object instead of Guid in Relation
/// </summary>
public class DtoClassInfoPost : IMtoClassInfo
{
    public PostType postType;
    public string postTypeClassName;

    public List<DtFieldInfo> fieldInfos;

    public IEnumerable<string> namespaces;

    public DtoClassInfoPost(PostType postType, IMetaModelTypesLocator mlocator)
    {
        this.postType = postType;
        this.postTypeClassName = GetPostTypeMtoName(postType.TypeName);

        fieldInfos = GenClassMto.PostTypeAsDtoClassSourceCode(postType, mlocator);

        this.namespaces = fieldInfos.Select(s => s.type.Namespace)!;
    }

    public static string GetPostTypeMtoName(string postTypeName)
    {
        return postTypeName[0].ToString().ToUpper() + postTypeName.Substring(1) + "Dto";
    }

    public void WriteTo(StringBuilder sb, bool closeBrackets = true)
    {

        List<Type> inherit = new() { typeof(Post), typeof(IDtoMarker) };
        if (postType.EnabledFeatures.Contains(nameof(Post.Status)))
        {
            inherit.Add(typeof(IMtoStatusProp));
        }
        if (postType.EnabledFeatures.Contains(nameof(Post.Likes)))
        {
            inherit.Add(typeof(IMtoLikes));
        }

        sb.AppendLine($"[Display(Name= \"{postType.Title}\")]");
        sb.AppendLine($"public partial class {postTypeClassName}: {string.Join(", ", inherit)}");
        sb.AppendLine("{");

        //var ff = TypeFieldsAsSourceCode(tt);
        //sb.AppendJoin('\n', ff);

        sb.AppendLine($"\tpublic static readonly string _typeName = \"{postType.TypeName}\";");

        sb.AppendLine("\n");
        sb.AppendJoin("\n\n", fieldInfos);

        //--extrafields
        if (postType.EnabledFeatures.Contains(nameof(Post.Status)))
        {
            sb.AppendLine("\n");
            sb.AppendLine("\t[Display(Name = \"PostStatus\")]");
            sb.AppendLine("\tpublic PostStatus PostStatus { get; set; } = default!;");
        }

        if (postType.EnabledFeatures.Contains(nameof(Post.Likes)))
        {
            sb.AppendLine("\n");
            sb.AppendLine("\t[Display(Name = \"IsLiked\")]");
            sb.AppendLine("\tpublic bool IsLiked { get; set; }");
        }
        //--extrafields

        sb.AppendLine("\n");
        AddConstructor(sb);
        sb.AppendLine("\n");
        AddConstructorFromMto(sb);
        sb.AppendLine("\n");

        if (closeBrackets)
        {
            sb.AppendLine("\n");
            sb.AppendLine("}");
        }
    }

    public void WriteSelectExpression(StringBuilder sb)
    {
        /*
        Func<Post, testPost> exp = post => new testPost
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            int1 = post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof(testPost.int1)).Int,
            str1 = post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof(testPost.str1)).StringShort
        };
        */

        sb.AppendLine($"\tpublic static readonly Expression<Func<Post, {postTypeClassName}>> selectExpression = post => new {postTypeClassName}");
        sb.AppendLine("\t{");

        PropertyInfo[] props = typeof(Post).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo prop in props)
        {
            if (prop.Name == "MetaValues") continue;

            //if(prop.Name == "User")
            //{
            //    sb.AppendLine($"\t\t{prop.Name} =  new UserDto(post.{prop.Name}),");
            //    continue;
            //}

            sb.AppendLine($"\t\t{prop.Name} =  post.{prop.Name},");
        }

        sb.AppendLine();
        sb.AppendLine("\t\t//internal fields");

        //if (postType.EnabledFeatures.Contains(nameof(Post.Likes)))
        //{
        //    sb.AppendLine("\t\tIsLiked = false, ");
        //}


        sb.AppendLine();
        sb.AppendLine("\t\t//Extra fields");

        foreach (var f in fieldInfos)
        {
            //sb.AppendLine($"\t\t{f.name} = post.MetaValues.FirstOrDefault(" +
            //    $"s => s.MetaField.Key == nameof({postTypeClassName}.{f.name}) && post.ParentId == Guid.Empty)?.{MetaValue.GetColName(f.metaField.Type)} ?? default,");

            var _val = $"post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof({postTypeClassName}.{f.name}) && s.MetaField.ParentId == Guid.Empty)";

            if (f.metaField.TypeRelation)
            {
                sb.AppendLine($"\t\t_{f.name} = (({_val}) == null) ? default :  ({_val}.{MetaValue.GetColName(f.metaField.Type)}),");
            }
            else
            {
                sb.AppendLine($"\t\t{f.name} = (({_val}) == null) ? default :  ({_val}.{MetaValue.GetColName(f.metaField.Type)}),");
            }
        }

        sb.AppendLine("\t};");
    }

    public void WriteRelationFieldsInfo(StringBuilder sb)
    {
        sb.AppendLine($"\tpublic static string[] _RelationFields = {{ {fieldInfos.Where(s => s.metaField.TypeRelation).Select(s => $"\"{s.name}\"").JoinStr(", ")} }};");

        sb.AppendLine($"\tpublic void Fill(Dictionary<Guid, MetaRelationObjectDict> dataDict)");
        sb.AppendLine("\t{");

        PropertyInfo[] props = typeof(Post).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var f in fieldInfos.Where(s => s.metaField.TypeRelation))
        {
            //if(dataDict.ContainsKey(_cat)) this.cat = Convert.ChangeType(dataDict[_cat].Entity, typeof(CategoryMto)) as CategoryMto;
            //string className = GetPostTypeMtoName(f.relationModelTypeFullName)
            //sb.AppendLine($"\t\tif(dataDict.ContainsKey(_{f.name})) this.{f.name} = Convert.ChangeType(dataDict[_{f.name}].Entity, typeof({f.relationModelTypeFullName})) as {f.relationModelTypeFullName};");
            //sb.AppendLine($"\t\tif(dataDict.ContainsKey(_{f.name})) this.{f.name} = dataDict[_{f.name}].Entity as {f.relationModelTypeFullName};");
            sb.AppendLine($"\t\tif(dataDict.ContainsKey(_{f.name})) this.{f.name} = ({f.relationModelTypeFullName})(dataDict[_{f.name}].Entity);");

        }
        sb.AppendLine("\t}");
    }

    void AddConstructor(StringBuilder sb)
    {
        sb.AppendLine($"\tpublic {postTypeClassName}()");
        sb.AppendLine("\t{\n\t}");
    }

    void AddConstructorFromMto(StringBuilder sb)
    {
        sb.AppendLine($"\tpublic {postTypeClassName} ({MtoClassInfoPost.GetPostTypeMtoName(postType.TypeName)} post, EfQueryFillContext fillCtx = null)");
        sb.AppendLine("\t{");
        sb.AppendLine("\t\tvar dataDict = fillCtx.FillDict;\n");

        PropertyInfo[] props = typeof(Post).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo prop in props)
        {
            if (prop.Name == "MetaValues") continue;

            //if(prop.Name == "User")
            //{
            //    sb.AppendLine($"\t\t{prop.Name} =  new UserDto(post.{prop.Name}),");
            //    continue;
            //}

            sb.AppendLine($"\t\t{prop.Name} = post.{prop.Name};");
        }

        sb.AppendLine($"\n\n\t\t//ctor extra fields\n");
        //sb.AppendLine($"\t/*");



        foreach (var f in fieldInfos)
        {

            if (f.metaField.TypeRelation)
            {
                sb.AppendLine($"\t\t_{f.name} = post.{f.name};");
                //Dictionary<string, string> dataDict;
                //dataDict.ContainsKey

                //sb.AppendLine($"Console.WriteLine($\"f={f.name} is null {{dataDict is null}}\");");

                sb.AppendLine($"\t\tif(dataDict?.ContainsKey(_{f.name}) ?? false)");
                //sb.AppendLine($"\t\t\t{f.name} = dataDict[post.Id].Entity as {f.relationModelTypeFullName};");
                sb.AppendLine($"\t\t\t{f.name} = dataDict[_{f.name}].Entity as {f.relationModelTypeFullName};");
                //sb.AppendLine($"\t\t{f.relationModelTypeFullName} = post.{f.name};");
            }
            else
            {
                sb.AppendLine($"\t\t{f.name} = post.{f.name};");
            }
        }

        //sb.AppendLine($"\t*/");

        sb.AppendLine("\t}");
    }
}
