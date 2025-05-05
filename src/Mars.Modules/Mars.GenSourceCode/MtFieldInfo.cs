using AppShared.Models;
using Mars.Host.Shared.Services;
using Mars.Core.Extensions;

namespace Mars.GenSourceCode;

public class MtFieldInfo
{
    public string name;
    public List<string> attributes = new();
    public string comment = "";
    public Type type;
    public bool isRelation;
    public MetaField metaField;

    protected bool IsSystemType => type.Namespace == "System";

    //string FriendlyTypeName => IsSystemType ? GetFriendlyTypeNames(type.Name) : type.FullName!;
    public string friendlyTypeName;
    public string? relationModelTypeFullName;

    public MtFieldInfo(MetaField metaField, IMetaModelTypesLocator mlocator)
    {

        this.metaField = metaField;
        this.name = metaField.Key;

        var mt = MetaField.MetaFieldTypeToType(metaField.Type);
        this.type = mt;

        this.friendlyTypeName = IsSystemType ? GenClassMto.GetFriendlyTypeName(type.Name) : type.FullName!;

        if (type.IsGenericType)
        {
            friendlyTypeName = GenClassMto.GetFormattedName(type);
        }
        else if (metaField.TypeRelation)
        {
            var t = mlocator.GetModelType(metaField.Type, metaField.ModelName);
            if (t != typeof(Post))
            {
                relationModelTypeFullName = t.FullName;
            }
            else
            {
                //Type mtoType = mlocator.MetaMtoModelsCompiledTypeDict[metaField.ModelName];
                //relationModelTypeFullName = mtoType.FullName;
                relationModelTypeFullName = MtoClassInfoPost.GetPostTypeMtoName(metaField.ModelName);
            }
        }

        attributes.Add($"[Display(Name=\"{metaField.Title}\")]");

        if (!string.IsNullOrEmpty(metaField.Description))
        {
            comment += ($"\t/// {metaField.Description}");
        }

        if (metaField.Hidden)
        {
            comment += ($"\t/// <i>hidden</i>");
        }

        if (!string.IsNullOrEmpty(comment))
        {
            comment = ($"\t/// <summary>\r\n{comment} \r\n\t/// </summary>");
        }
    }

    public override string ToString()
    {
        return
            (string.IsNullOrEmpty(comment) ? "" : $"{comment}\n") +
            $"\t{attributes.JoinStr("\n\t")}\n" +
            $"\tpublic {friendlyTypeName} {name} {{get; set; }}";
    }
}
