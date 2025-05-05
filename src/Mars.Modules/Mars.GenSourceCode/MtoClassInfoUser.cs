using System.Reflection;
using System.Text;
using AppShared.Models;
using Mars.Host.Shared.Services;

namespace Mars.GenSourceCode;

public class MtoClassInfoUser : IMtoClassInfo
{
    public string UserTypeClassName;

    public List<MtFieldInfo> fieldInfos;

    public IEnumerable<string> namespaces;

    public List<MetaField> MetaFields { get; }

    public MtoClassInfoUser(List<MetaField> metaFields, IMetaModelTypesLocator mlocator)
    {
        this.UserTypeClassName = GetUserTypeMtoName();

        fieldInfos = UserTypeAsClassSourceCode(metaFields, mlocator);

        this.namespaces = fieldInfos.Select(s => s.type.Namespace)!;
        MetaFields = metaFields;
    }

    internal static List<MtFieldInfo> UserTypeAsClassSourceCode(List<MetaField> metaFields, IMetaModelTypesLocator mlocator)
    {
        List<MtFieldInfo> result = new();

        foreach (var metaField in metaFields.Where(s => s.ParentId == Guid.Empty))
        {
            if (metaField.TypeParentable) continue; //ignore parentable 

            if (metaField.Disable) continue;


            MtFieldInfo tt = new MtFieldInfo(metaField, mlocator);

            result.Add(tt);
        }

        return result;

    }

    string GetUserTypeMtoName()
    {
        return "UserMto";
    }

    public void WriteTo(StringBuilder sb, bool closeBrackets = true)
    {

        //sb.AppendLine($"[Display(Name= \"{userType.Title}\")]");
        sb.AppendLine($"public partial class {UserTypeClassName}: User, IMtoMarker");
        sb.AppendLine("{");

        //sb.AppendLine($"\tpublic static readonly string _typeName = \"{userType.TypeName}\";");

        sb.AppendLine("\n");
        sb.AppendJoin("\n\n", fieldInfos);

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
        Func<User, testUser> exp = user => new testUser
        {
            Id = user.Id,
            Title = user.Title,
            Content = user.Content,
            int1 = user.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof(testUser.int1)).Int,
            str1 = user.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof(testUser.str1)).StringShort
        };
        */

        sb.AppendLine($"\tpublic static readonly Func<User, {UserTypeClassName}> selectExpression = user => new {UserTypeClassName}");
        sb.AppendLine("\t{");

        PropertyInfo[] props = typeof(User).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite).ToArray();

        foreach (PropertyInfo prop in props)
        {
            sb.AppendLine($"\t\t{prop.Name} =  user.{prop.Name},");
        }

        sb.AppendLine();
        sb.AppendLine("\t\t//Extra fields");

        foreach (var f in fieldInfos)
        {
            //sb.AppendLine($"\t\t{f.name} = user.MetaValues.FirstOrDefault(" +
            //    $"s => s.MetaField.Key == nameof({UserTypeClassName}.{f.name}))?.{MetaValue.GetColName(f.metaField.Type)} && default,");

            var _val = $"user.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof({UserTypeClassName}.{f.name}) && s.MetaField.ParentId == Guid.Empty)";

            sb.AppendLine($"\t\t{f.name} = (({_val}) == null) ? default :  ({_val}.{MetaValue.GetColName(f.metaField.Type)}),");
        }

        sb.AppendLine("\t};");
    }

    void AddConstructor(StringBuilder sb)
    {
        sb.AppendLine($"\tpublic {UserTypeClassName}()");
        sb.AppendLine("\t{\n\t}\n");
    }
}