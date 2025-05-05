using System.Reflection;
using System.Text;
using AppShared.Dto;
using AppShared.Models;
using Mars.Host.Shared.Services;

namespace Mars.GenSourceCode;

public class MetaFieldGroupClassInfo : IMtoClassInfo
{
    public PostType postType;
    MetaField groupField;
    public string groupFieldClassName;

    public List<MtFieldInfo> fieldInfos;

    public IEnumerable<string> namespaces;

    public MetaFieldGroupClassInfo(PostType postType, MetaField groupField, IMetaModelTypesLocator mlocator)
    {
        this.postType = postType;
        this.groupField = groupField;

        this.groupFieldClassName = GetGroupFieldMtoName(postType.TypeName);

        fieldInfos = GenClassMto.PostTypeAsMtoClassSourceCode(postType, mlocator, groupField.Id);

        //TODO: STAGE пока только сгенерил класс

        this.namespaces = fieldInfos.Select(s => s.type.Namespace)!;
    }

    public string GetGroupFieldMtoName(string postTypeName)
    {
        return postTypeName[0].ToString().ToUpper() + postTypeName.Substring(1) + "Mto" + "Group_" + PathToField();
    }

    string PathToField()
    {
        return groupField.Key;
    }

    public void WriteTo(StringBuilder sb, bool closeBrackets = true)
    {

        sb.AppendLine($"[Display(Name= \"{groupField.Title}\")]");
        sb.AppendLine($"public partial class {groupFieldClassName}: IMtoGroupMarker");
        sb.AppendLine("{");

        //var ff = TypeFieldsAsSourceCode(tt);
        //sb.AppendJoin('\n', ff);

        sb.AppendLine($"\tpublic static readonly string _parentTypeName = \"{postType.TypeName}\";");

        sb.AppendJoin("\n\n", fieldInfos);

        if (closeBrackets)
        {
            sb.AppendLine("\n");
            sb.AppendLine("}");
        }
    }

    public void WriteSelectExpression(StringBuilder sb)
    {
        ///*
        //Func<Post, testPost> exp = post => new testPost
        //{
        //    Id = post.Id,
        //    Title = post.Title,
        //    Content = post.Content,
        //    int1 = post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof(testPost.int1)).Int,
        //    str1 = post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof(testPost.str1)).StringShort
        //};
        //*/

        //sb.AppendLine($"\tpublic static readonly Expression<Func<Post, {groupFieldClassName}>> selectExpression = post => new {groupFieldClassName}");
        //sb.AppendLine("\t{");

        //PropertyInfo[] props = typeof(Post).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        //foreach (PropertyInfo prop in props)
        //{
        //    if (prop.Name == "MetaValues") continue;

        //    //if(prop.Name == "User")
        //    //{
        //    //    sb.AppendLine($"\t\t{prop.Name} =  new UserDto(post.{prop.Name}),");
        //    //    continue;
        //    //}

        //    sb.AppendLine($"\t\t{prop.Name} =  post.{prop.Name},");
        //}

        //sb.AppendLine();
        //sb.AppendLine("\t\t//Extra fields");

        //foreach (var f in fieldInfos)
        //{
        //    //sb.AppendLine($"\t\t{f.name} = post.MetaValues.FirstOrDefault(" +
        //    //    $"s => s.MetaField.Key == nameof({postTypeClassName}.{f.name}) && post.ParentId == Guid.Empty)?.{MetaValue.GetColName(f.metaField.Type)} ?? default,");

        //    var _val = $"post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof({groupFieldClassName}.{f.name}) && s.MetaField.ParentId == Guid.Empty)";

        //    sb.AppendLine($"\t\t{f.name} = (({_val}) == null) ? default :  ({_val}.{MetaValue.GetColName(f.metaField.Type)}),");
        //}

        //sb.AppendLine("\t};");
    }
}
