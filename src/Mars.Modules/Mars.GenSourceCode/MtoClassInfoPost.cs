using System.Reflection;
using System.Text;
using AppShared.Dto;
using AppShared.Models;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Services;

namespace Mars.GenSourceCode;

public class MtoClassInfoPost : IMtoClassInfo
{
    public PostType postType;
    public string postTypeClassName;

    public List<MtFieldInfo> fieldInfos;

    public IEnumerable<string> namespaces;

    public MtoClassInfoPost(PostType postType, IMetaModelTypesLocator mlocator)
    {
        this.postType = postType;
        this.postTypeClassName = GetPostTypeMtoName(postType.TypeName);

        fieldInfos = GenClassMto.PostTypeAsMtoClassSourceCode(postType, mlocator, Guid.Empty);

        this.namespaces = fieldInfos.Select(s => s.type.Namespace)!;
    }

    public static string GetPostTypeMtoName(string postTypeName)
    {
        return postTypeName[0].ToString().ToUpper() + postTypeName.Substring(1) + "Mto";
    }

    public void WriteTo(StringBuilder sb, bool closeBrackets = true)
    {
        List<Type> inherit = new() { typeof(Post), typeof(IMtoMarker) };
        if (postType.EnabledFeatures.Contains(nameof(Post.Status)))
        {
            inherit.Add(typeof(IMtoStatusProp));
        }

        sb.AppendLine($"[Display(Name = \"{postType.Title}\")]");
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
            sb.AppendLine("\tpublic PostStatus PostStatus { get; set; }");
        }
        //--extrafields

        sb.AppendLine("\n");
        AddConstructor(sb);
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

        //not used
        ////if (postType.EnabledFeatures.Contains(nameof(Post.Status)))
        ////{
        ////    sb.AppendLine("\t\t//status");
        ////    sb.AppendLine($"\t\t{nameof(IMtoStatusProp.PostStatus)} = ");
        ////}

        sb.AppendLine();
        sb.AppendLine("\t\t//Extra fields");

        foreach (var f in fieldInfos)
        {
            //sb.AppendLine($"\t\t{f.name} = post.MetaValues.FirstOrDefault(" +
            //    $"s => s.MetaField.Key == nameof({postTypeClassName}.{f.name}) && post.ParentId == Guid.Empty)?.{MetaValue.GetColName(f.metaField.Type)} ?? default,");

            //if (f.metaField.Type == EMetaFieldType.Select)
            //{
            //    //MetaFieldVariant
            //    var _val = $"post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof({postTypeClassName}.{f.name}) && s.MetaField.ParentId == Guid.Empty)";
            //    //VariantId
            //    //Variants
            //    var _selectId = $"{_val}.{MetaValue.GetColName(f.metaField.Type)}";
            //    var _s = $"post.MetaField";
            //    sb.AppendLine($"\t\t{f.name} = (({_val}) == null) ? default :  ({_s}),");
            //}
            //else
            {
                var _val = $"post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof({postTypeClassName}.{f.name}) && s.MetaField.ParentId == Guid.Empty)";
                sb.AppendLine($"\t\t{f.name} = (({_val}) == null) ? default :  ({_val}.{MetaValue.GetColName(f.metaField.Type)}),");
            }
        }

        sb.AppendLine("\t};");
    }

    void AddConstructor(StringBuilder sb)
    {
        sb.AppendLine($"\tpublic {postTypeClassName}()");
        sb.AppendLine("\t{\n\t}\n");
    }
}
